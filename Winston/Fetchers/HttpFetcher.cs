﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Winston.Net;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Fetchers
{
    public class HttpFetcher : IPackageFetcher
    {
        public async Task<TempPackage> FetchAsync(Package pkg, Progress progress)
        {
            var result = new TempPackage { WorkDirectory = new TempDirectory() };
            var actualLocation = pkg.Location;
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = NetUtils.HttpClient(handler))
            {
                // Do an HTTP HEAD request and follow any 301 redirect
                using (var req = new HttpRequestMessage(HttpMethod.Head, pkg.Location))
                {
                    var res = await client.SendAsync(req);
                    if (res.Headers.Location != null)
                    {
                        // TODO: handle multiple levels of redirect (rare?)
                        actualLocation = res.Headers.Location;
                    }
                }

                // Download the file
                using (var response = await client.GetAsync(actualLocation, HttpCompletionOption.ResponseHeadersRead))
                {
                    result.MimeType = response.Content.Headers.ContentType?.MediaType;

                    // Try to get the right file name to give hints about the file type to the extractor
                    result.FileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('\"', '\\', '\'') ??
                                      actualLocation.LastSegment() ??
                                      pkg.Filename ??
                                      "package";

                    var total = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var output = File.Open(result.FullPath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[81920]; // Default size from .NET docs on CopyTo
                        while (true)
                        {
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                break;
                            }
                            await output.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            // Can't report progress if there was no Content-Length
                            if (total > 0)
                            {
                                progress?.Report(totalRead*1d/(total*1d)*100);
                            }
                        }
                        progress?.Report(100);
                        progress?.CompletedDownload();
                    }
                }

                var hash = await FileSystem.GetSha1Async(result.FullPath);
                // Only check when Sha1 is specified in the package metadata
                if (!string.IsNullOrWhiteSpace(pkg.Sha1) &&
                    !string.Equals(hash, pkg.Sha1, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA1 hash of remote file {pkg.Location} did not match {pkg.Sha1}");
                }
            }
            return result;
        }

        public bool IsMatch(Package pkg)
        {
            return pkg.Location.Scheme == "http" || pkg.Location.Scheme == "https";
        }
    }
}