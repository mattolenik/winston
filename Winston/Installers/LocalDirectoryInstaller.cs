using System;
using System.IO;
using System.Threading.Tasks;
using Winston.OS;

namespace Winston.Installers
{
    class LocalDirectoryInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            // TODO: check for a pkg.json to verify this directory is really a package?
            return Directory.Exists(pkg.URL.LocalPath) ? new LocalDirectoryInstaller(pkg, pkgDir) : null;
        }

        LocalDirectoryInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> Install(Progress progress)
        {
            var installDir = Path.Combine(pkgDir, pkg.ResolveVersion() ?? "default");
            await FS.CopyDirectory(pkg.URL.LocalPath, installDir, progress);
            return new DirectoryInfo(installDir);
        }

        public Task<Exception> Validate()
        {
            return Task.FromResult(null as Exception);
        }

        public void Dispose() { }
    }
}
