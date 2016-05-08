using System.Threading.Tasks;
using Winston.Fetchers;
using Winston.OS;

namespace Winston.Extractors
{
    internal class LocalDirectoryExtractor : IPackageExtractor
    {
        public async Task ExtractAsync(TempPackage package, string destination, Progress progress)
        {
            await FileSystem.CopyDirectoryAsync(package.PackageItem.Path, destination, progress);
        }

        public bool IsMatch(TempPackage package)
        {
            return package.PackageItem is TempDirectory;
        }
    }
}