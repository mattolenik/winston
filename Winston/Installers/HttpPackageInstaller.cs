using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using fastJSON;
using Winston.OS;
using Winston.Serialization;

namespace Winston.Installers
{
    class HttpPackageInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;
        readonly TempFile tmpFile;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            if (pkg.URL.Scheme == "http" || pkg.URL.Scheme == "https")
            {
                return new HttpPackageInstaller(pkg, pkgDir);
            }
            return null;
        }

        HttpPackageInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
            tmpFile = new TempFile();
        }

        public async Task<DirectoryInfo> Install(Progress progress)
        {
            using (var c = new WebClient())
            {
                c.DownloadProgressChanged += (sender, args) => progress.UpdateDownload(args.ProgressPercentage);
                await c.DownloadFileTaskAsync(pkg.URL, tmpFile);
                progress.CompletedDownload();

                var hash = await FS.GetSHA1(tmpFile);
                // Only check when SHA1 is specified in the package metadata
                if (!string.IsNullOrWhiteSpace(pkg.SHA1) &&
                    !string.Equals(hash, pkg.SHA1, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA1 hash of remote file {pkg.URL} did not match {pkg.SHA1}");
                }
                var version = pkg.ResolveVersion() ?? hash;

                // Save package information to disk first. Other actions can use this
                // to interact with a package without having to load whole repos into memory.
                Directory.CreateDirectory(pkgDir);
                File.WriteAllText(Path.Combine(pkgDir, "pkg.json"), JSON.ToJSON(pkg));

                // TODO: replace hash with version resolution
                var installDir = Path.Combine(pkgDir, version);
                Directory.CreateDirectory(installDir);

                IFileExtractor archive;

                switch (pkg?.FileType)
                {
                    case PackageFileType.Archive:
                        archive = ArchiveExtractor.TryCreate(pkg, installDir, tmpFile, c.ResponseHeaders, pkg.URL);
                        break;

                    case PackageFileType.Binary:
                        archive = ExeExtractor.TryCreate(pkg, installDir, tmpFile, c.ResponseHeaders, pkg.URL);
                        break;

                    default:
                        archive = ArchiveExtractor.TryCreate(pkg, installDir, tmpFile, c.ResponseHeaders, pkg.URL) ??
                                      ExeExtractor.TryCreate(pkg, installDir, tmpFile, c.ResponseHeaders, pkg.URL);
                        break;
                }

                if (archive != null)
                {
                    var p = await archive.Install(progress);
                    progress.CompletedInstall();
                }
                else
                {
                    throw new NotSupportedException("Unable to identify type of package at URL: " + pkg.URL);
                }

                return new DirectoryInfo(installDir);
            }
        }


        public Task<Exception> Validate()
        {
            return Task.FromResult(null as Exception);
        }

        public void Dispose() => tmpFile?.Dispose();
    }
}
