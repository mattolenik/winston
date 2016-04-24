using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Winston.OS;
using static Winston.OS.SimpleProcess;
using Winston.Packaging;

namespace Winston.Installers
{
    class MsiExtractor : IPackageExtractor
    {
        Package pkg;
        string installDir;
        string packageFile;

        public static IPackageExtractor TryCreate(Package pkg, string installDir, string packageFile)
        {
            var result = new MsiExtractor
            {
                pkg = pkg,
                installDir = installDir,
                packageFile = packageFile
            };
            var ext = Path.GetExtension(packageFile)?.ToLowerInvariant();
            if (ext != ".msi") { return null; }
            return result;
        }

        public async Task<string> InstallAsync(Progress progress)
        {
            var p = await Cmd($"msiexec /a {packageFile} /qn TARGETDIR=\"{installDir}\"").RunAsync();
            if (p.ExitCode != 0)
            {
                throw p.GetException();
            }
            return Path.Combine(installDir, pkg.Name);
        }
    }
}