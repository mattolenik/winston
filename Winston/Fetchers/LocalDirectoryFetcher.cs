using System.IO;
using System.Threading.Tasks;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Fetchers
{
    class LocalDirectoryFetcher : IPackageFetcher
    {
        public async Task<TempPackage> FetchAsync(Package pkg, Progress progress)
        {
            var result = new TempPackage
            {
                FileName = Path.GetFileName(pkg.Location.LocalPath),
                Package = pkg,
                WorkDirectory = new NonTempItem(pkg.Location.LocalPath)
            };
            return await Task.FromResult(result);
        }

        public bool IsMatch(Package pkg)
        {
            // TODO: check for a pkg.json to verify this directory is really a package?
            return Directory.Exists(pkg?.Location?.LocalPath ?? ":");
        }
    }
}