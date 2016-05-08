using System;
using System.IO;
using System.Threading.Tasks;
using Winston.Fetchers;

namespace Winston.Extractors
{
    public class ExeExtractor : IPackageExtractor
    {
        public bool IsMatch(TempPackage package)
        {
            return Path.GetExtension(package.FileName ?? "").EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task ExtractAsync(TempPackage package, string destination, Progress progress)
        {
            var exePath = Path.Combine(destination, package.FileName);
            await Task.Run(() =>
            {
                File.Delete(exePath);
                File.Move(package.PackageItem.Path, exePath);
            });
        }
    }
}