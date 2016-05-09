using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Fetchers
{
    public class HttpFetcher : IPackageFetcher
    {
        public async Task<TempPackage> FetchAsync(Package pkg, Progress progress)
        {
            var result = new TempPackage();
            var actualLocation = pkg.Location;
            // Do an HTTP HEAD request and follow any 301 redirect
            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client = new HttpClient(handler))
            using (var req = new HttpRequestMessage(HttpMethod.Head, pkg.Location))
            {
                var res = await client.SendAsync(req);
                if (res.StatusCode == HttpStatusCode.Moved)
                {
                    if (res.Headers.Location == null)
                    {
                        throw new InvalidOperationException($"No Location header found in HEAD request to {pkg.Location}");
                    }
                    actualLocation = res.Headers.Location;
                }
            }
            var ext = Path.GetExtension(actualLocation.AbsolutePath);

            var tmpFile = new TempFile(ext);
            result.PackageItem = tmpFile;
            using (var webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += (sender, args) => progress.UpdateDownload(args.ProgressPercentage);
                await webClient.DownloadFileTaskAsync(actualLocation, tmpFile);
                progress.CompletedDownload();

                var hash = await FileSystem.GetSha1Async(tmpFile);
                // Only check when Sha1 is specified in the package metadata
                if (!string.IsNullOrWhiteSpace(pkg.Sha1) &&
                    !string.Equals(hash, pkg.Sha1, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA1 hash of remote file {pkg.Location} did not match {pkg.Sha1}");
                }
                result.FileName = FileNameFromHeader(webClient.ResponseHeaders) ?? actualLocation.LastSegment() ?? pkg.Filename;
                result.MimeType = webClient.ResponseHeaders["Extensions-Type"];
            }
            return result;
        }

        public bool IsMatch(Package pkg)
        {
            return pkg.Location.Scheme == "http" || pkg.Location.Scheme == "https";
        }

        static string FileNameFromHeader(NameValueCollection headers)
        {
            var disposition = headers?["Extensions-Disposition"] ?? "-";
            var cd = new ContentDisposition(disposition);
            return cd.FileName;
        }
    }
}