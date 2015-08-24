using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class LocalDirectoryInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            // TODO: check for a pkg.yml to verify this directory is really a package?
            return Directory.Exists(pkg.URL.LocalPath) ? new LocalDirectoryInstaller(pkg, pkgDir) : null;
        }

        LocalDirectoryInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> Install()
        {
            var installDir = Path.Combine(pkgDir, pkg.ResolveVersion() ?? "default");
            await OS.CopyDirectory(pkg.URL.LocalPath, installDir);
            return new DirectoryInfo(installDir);
        }

        public Task<Exception> Validate()
        {
            return Task.FromResult(null as Exception);
        }

        public void Dispose() { }
    }
}
