using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Winston.Installers;

namespace Winston.Packaging
{
    public class PackageClient
    {
        readonly Package pkg;
        readonly string pkgDir;

        public PackageClient(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
        }

        public async Task<DirectoryInfo> InstallAsync(Progress progress)
        {
            var installer = LocalDirectoryInstaller.TryCreate(pkg, pkgDir) ??
                            HttpPackageInstaller.TryCreate(pkg, pkgDir);

            if (installer == null)
            {
                throw new InvalidOperationException($"Could not find suitable installer for package {pkg}");
            }
            var installPath = await installer.InstallAsync(progress);
            var error = await installer.ValidateAsync();
            if (error != null)
            {
                throw error;
            }
            return installPath;
        }
    }
}