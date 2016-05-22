using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winston.Extractors;
using Winston.Fetchers;
using Winston.OS;
using Winston.Serialization;

namespace Winston.Packaging
{
    public class PackageClient
    {
        readonly Package pkg;
        readonly string pkgDir;

        readonly IPackageFetcher[] fetchers =
        {
            new LocalDirectoryFetcher(),
            new HttpFetcher(),
            new GithubFetcher()
        };

        readonly Dictionary<PackageType, IPackageExtractor[]> extractors = new Dictionary<PackageType, IPackageExtractor[]>
        {
            {PackageType.Archive, new IPackageExtractor[] {new ArchiveExtractor()}},
            {PackageType.Binary, new IPackageExtractor[] {new ExeExtractor()}},
            {PackageType.Setup, new IPackageExtractor[] {new MsiExtractor()}},
            {PackageType.LocalDirectory, new IPackageExtractor[] {new LocalDirectoryExtractor()}},
            {PackageType.Unspecified, new IPackageExtractor[] {new ExeExtractor(), new ArchiveExtractor(), new MsiExtractor()}}
        };


        public PackageClient(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> InstallAsync(Progress progress)
        {
            var fetcher = fetchers.FirstOrDefault(f => f.IsMatch(pkg));
            if (fetcher == null)
            {
                throw new NotSupportedException($"Could not fetch package '{pkg}'");
            }
            var tmpPkg = await fetcher.FetchAsync(pkg, progress);
            string hash = null;
            if (File.Exists(tmpPkg.FullPath))
            {
                // FullPath exists in most cases, but is absent for directory install
                hash = await FileSystem.GetSha1Async(tmpPkg.FullPath);
            }
            // Only check when Sha1 is specified in the package metadata
            if (!string.IsNullOrWhiteSpace(pkg.Sha1) &&
                !string.Equals(hash, pkg.Sha1, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"SHA1 hash of remote file {pkg.Location} did not match {pkg.Sha1}");
            }
            var version = pkg.ResolveVersion() ?? hash ?? "default";

            // Save package information to disk first
            Directory.CreateDirectory(pkgDir);
            Json.Save(pkg, Path.Combine(pkgDir, "pkg.json"), true);

            // TODO: replace hash with version resolution
            var installDir = Path.Combine(pkgDir, version);
            Directory.CreateDirectory(installDir);

            var extractor = extractors[pkg.Type].FirstOrDefault(e => e.IsMatch(tmpPkg));
            if (extractor == null)
            {
                throw new NotSupportedException($"Could not extract package '{pkg}'");
            }
            await extractor.ExtractAsync(tmpPkg, installDir, progress);
            progress.CompletedInstall();
            return new DirectoryInfo(installDir);
        }
    }
}