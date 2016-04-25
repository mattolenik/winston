using System;
using System.IO;
using System.Threading.Tasks;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Installers
{
    class LocalDirectoryInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            // TODO: check for a pkg.json to verify this directory is really a package?
            return Directory.Exists(pkg?.Location?.LocalPath) ? new LocalDirectoryInstaller(pkg, pkgDir) : null;
        }

        LocalDirectoryInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> InstallAsync(Progress progress)
        {
            var installDir = Path.Combine(pkgDir, pkg.ResolveVersion() ?? "default");
            await FileSystem.CopyDirectoryAsync(pkg.Location.LocalPath, installDir, progress);
            return new DirectoryInfo(installDir);
        }

        public Task<Exception> ValidateAsync()
        {
            return Task.FromResult(null as Exception);
        }
    }
}
