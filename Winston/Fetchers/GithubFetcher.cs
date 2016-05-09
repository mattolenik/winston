using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Winston.Net;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Fetchers
{
    class GithubFetcher : IPackageFetcher
    {
        public async Task<TempPackage> FetchAsync(Package pkg, Progress progress)
        {
            var user = pkg.Location.Host;
            var project = pkg.Location.Segments.Skip(1).SingleOrDefault();
            var props = pkg.Location.ParseQueryString();
            var url = $"https://api.github.com/repos/{user}/{project}/releases/latest";

            using (var httpClient = NetUtils.HttpClient())
            using (var webClient = NetUtils.WebClient())
            {
                var content = await httpClient.GetStringAsync(url);

                var json = fastJSON.JSON.ToDynamic(content);
                var pkgUrlString = json.assets[0].browser_download_url as string;
                Uri pkgUri;
                if (!Uri.TryCreate(pkgUrlString, UriKind.Absolute, out pkgUri))
                {
                    throw new UriFormatException($"Could not parse output from Github API, failed to parse URI: '{pkgUrlString}'");
                }
                var result = new TempPackage
                {
                    Package = pkg,
                    PackageItem = new TempFile(pkg.Name),
                    FileName = pkgUri.LastSegment()
                };

                webClient.DownloadProgressChanged += (sender, args) => progress.UpdateDownload(args.ProgressPercentage);
                await webClient.DownloadFileTaskAsync(pkgUri, result.PackageItem.Path);
                progress.CompletedDownload();
                return result;
            }
        }

        public bool IsMatch(Package pkg)
        {
            return pkg.Location.Scheme == "github";
        }
    }
}