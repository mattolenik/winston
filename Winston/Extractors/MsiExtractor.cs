using System;
using System.IO;
using System.Threading.Tasks;
using Winston.Fetchers;
using Winston.OS;

namespace Winston.Extractors
{
    public class MsiExtractor : IPackageExtractor
    {
        public bool IsMatch(TempPackage package)
        {
            return Path.GetExtension(package.FileName ?? "").EndsWith("msi", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task ExtractAsync(TempPackage package, string destination, Progress progress)
        {
            var p = await SimpleProcess.Cmd($"msiexec /a {package.FullPath} /qn TARGETDIR=\"{destination}\"").RunAsync();
            if (p.ExitCode != 0)
            {
                throw p.GetException();
            }
        }
    }
}