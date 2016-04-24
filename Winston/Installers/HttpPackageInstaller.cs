﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using fastJSON;
using Winston.OS;
using Winston.Packaging;
using Winston.Serialization;

namespace Winston.Installers
{
    class HttpPackageInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            if (pkg.Location.Scheme == "http" || pkg.Location.Scheme == "https")
            {
                return new HttpPackageInstaller(pkg, pkgDir);
            }
            return null;
        }

        HttpPackageInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> InstallAsync(Progress progress)
        {
            var actualLocation = pkg.Location;
            var headReq = WebRequest.Create(pkg.Location) as HttpWebRequest;
            headReq.Method = "HEAD";
            var headRes = (await headReq.GetResponseAsync()) as HttpWebResponse;
            if (headRes.ResponseUri != null)
            {
                actualLocation = headRes.ResponseUri;
            }
            var ext = Path.GetExtension(actualLocation.AbsolutePath);

            using (var webClient = new WebClient())
            using (var tmpFile = new TempFile(ext))
            {
                webClient.DownloadProgressChanged += (sender, args) => progress.UpdateDownload(args.ProgressPercentage);
                await webClient.DownloadFileTaskAsync(actualLocation, tmpFile);
                progress.CompletedDownload();

                var hash = await FS.GetSHA1Async(tmpFile);
                // Only check when Sha1 is specified in the package metadata
                if (!string.IsNullOrWhiteSpace(pkg.Sha1) &&
                    !string.Equals(hash, pkg.Sha1, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA1 hash of remote file {pkg.Location} did not match {pkg.Sha1}");
                }
                var version = pkg.ResolveVersion() ?? hash;

                // Save package information to disk first. Other actions can use this
                // to interact with a package without having to load whole repos into memory.
                Directory.CreateDirectory(pkgDir);
                File.WriteAllText(Path.Combine(pkgDir, "pkg.json"), JSON.ToJSON(pkg));

                // TODO: replace hash with version resolution
                var installDir = Path.Combine(pkgDir, version);
                Directory.CreateDirectory(installDir);

                IPackageExtractor extractor;

                switch (pkg?.FileType)
                {
                    case PackageFileType.Archive:
                        extractor =
                            ArchiveExtractor.TryCreate(pkg, installDir, tmpFile, webClient.ResponseHeaders, pkg.Location) ??
                            MsiExtractor.TryCreate(pkg, installDir, tmpFile);
                        break;

                    case PackageFileType.Binary:
                        extractor = ExeExtractor.TryCreate(pkg, installDir, tmpFile, webClient.ResponseHeaders, pkg.Location);
                        break;

                    default:
                        extractor = ArchiveExtractor.TryCreate(pkg, installDir, tmpFile, webClient.ResponseHeaders, pkg.Location) ??
                                      ExeExtractor.TryCreate(pkg, installDir, tmpFile, webClient.ResponseHeaders, pkg.Location);
                        break;
                }

                if (extractor != null)
                {
                    var p = await extractor.InstallAsync(progress);
                    progress.CompletedInstall();
                }
                else
                {
                    throw new NotSupportedException("Unable to identify type of package at Location: " + pkg.Location);
                }

                return new DirectoryInfo(installDir);
            }
        }

        public async Task<Exception> ValidateAsync()
        {
            return await Task.FromResult(null as Exception);
        }
    }
}