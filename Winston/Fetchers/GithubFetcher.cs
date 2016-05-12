﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
            var project = pkg.Location.Segments.Skip(1).SingleOrDefault()?.Trim('/');
            var props = pkg.Location.ParseQueryString();
            var url = $"https://api.github.com/repos/{user}/{project}/releases/latest";

            using (var httpClient = NetUtils.HttpClient())
            using (var webClient = NetUtils.WebClient())
            {
                var content = await httpClient.GetStringAsync(url);

                var json = fastJSON.JSON.Parse(content) as IDictionary<string, object>;
                var assets = json["assets"] as IList<object>;
                var asset = NarrowAssets(assets, props);
                var pkgUrlString = asset["browser_download_url"] as string;
                Uri pkgUri;
                if (!Uri.TryCreate(pkgUrlString, UriKind.Absolute, out pkgUri))
                {
                    throw new UriFormatException($"Could not parse output from Github API, failed to parse URI: '{pkgUrlString}'");
                }
                var result = new TempPackage
                {
                    Package = pkg,
                    WorkDirectory = new TempDirectory(),
                    FileName = pkgUri.LastSegment()
                };

                webClient.DownloadProgressChanged += (sender, args) => progress?.UpdateDownload(args.ProgressPercentage);
                await webClient.DownloadFileTaskAsync(pkgUri, result.FullPath);
                progress?.CompletedDownload();
                return result;
            }
        }

        public bool IsMatch(Package pkg)
        {
            return pkg.Location.Scheme == "github";
        }

        static IDictionary<string, object> NarrowAssets(IList<object> assets, IList<Tuple<string,string>>  query)
        {
            if (assets.Count == 1)
            {
                return assets[0] as IDictionary<string, object>;
            }
            var matches = new List<IDictionary<string, object>>();
            foreach (var asset in assets.Cast<IDictionary<string, object>>())
            {
                var match = true;
                foreach (var pair in query)
                {
                    var pattern = pair.Item2;

                    var inverted = false;
                    if (pattern.StartsWith("!"))
                    {
                        pattern = pattern.Substring(1);
                        inverted = true;
                    }
                    var value = asset[pair.Item1] as string;
                    var isLike = value.Like(pattern);
                    if (inverted)
                    {
                        isLike = !isLike;
                    }
                    match = match && isLike;
                }
                if (match)
                {
                    matches.Add(asset);
                }
            }
            if (matches.Count > 1)
            {
                throw new Exception("Could not decide from available assets, add a filter");
            }
            if (matches.Count == 0)
            {
                throw new Exception("Expected to find at least one asset");
            }
            return matches[0];
        }
    }
}