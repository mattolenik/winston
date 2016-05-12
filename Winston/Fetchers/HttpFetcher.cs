using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
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
            // Do an HTTP HEAD request and follow any 301 redirect
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = NetUtils.HttpClient(handler))
            using (var req = new HttpRequestMessage(HttpMethod.Head, pkg.Location))
            {
                var res = await client.SendAsync(req);
                if (res.StatusCode == HttpStatusCode.Moved)
                {
                    if (res.Headers.Location == null)
                    {
                        throw new InvalidOperationException($"No Location header found in HEAD request to {pkg.Location}");
                    }
                    // TODO: handle multiple levels of redirect (rare?)
                    actualLocation = res.Headers.Location;
                }
            }

            using (var webClient = NetUtils.WebClient())
            {
                var tmp = Path.GetTempFileName();
                webClient.DownloadProgressChanged += (sender, args) => progress?.UpdateDownload(args.ProgressPercentage);
                await webClient.DownloadFileTaskAsync(actualLocation, tmp);

                progress?.CompletedDownload();

                result.MimeType = webClient.ResponseHeaders["Content-Type"];

                // Try to get the right file name to give hints about the file type to the extractor
                result.FileName = FileNameFromHeader(webClient.ResponseHeaders) ??
                                  actualLocation.LastSegment() ??
                                  pkg.Filename ??
                                  "package";

                // It's hard to get the response headers without just waiting for the GET on the package to complete,
                // so just download to a temp file and move to the proper name when done.
                File.Move(tmp, result.FullPath);

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

        static string FileNameFromHeader(WebHeaderCollection headers)
        {
            var disposition = headers?["Content-Disposition"] ?? "-";
            var cd = new ContentDisposition(disposition);
            return cd.FileName;
        }
    }
}