using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fastJSON;
using Winston.Extractors;
using Winston.Fetchers;
using Winston.OS;

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

        readonly IDictionary<PackageType, IEnumerable<IPackageExtractor>> extractors;

        public PackageClient(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
            var archive = new ArchiveExtractor();
            var exe = new ExeExtractor();
            var msi = new MsiExtractor();
            var localDir = new LocalDirectoryExtractor();
            extractors = new Dictionary<PackageType, IEnumerable<IPackageExtractor>>
            {
                {PackageType.Archive, new[] {archive}},
                {PackageType.Binary, new[] {exe}},
                {PackageType.Setup, new[] {msi}},
                {PackageType.LocalDirectory, new[] {localDir}},
                {PackageType.Nil, new IPackageExtractor[] {localDir, exe, archive, msi}}
            };
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
            if (tmpPkg.PackageItem is TempFile)
            {
                hash = await FileSystem.GetSha1Async(tmpPkg.PackageItem.Path);
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
            File.WriteAllText(Path.Combine(pkgDir, "pkg.json"), JSON.ToJSON(pkg));

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