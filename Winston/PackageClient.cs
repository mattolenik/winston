using System;
using System.IO;
using System.Threading.Tasks;
using Winston.Installers;
using Winston.User;

namespace Winston
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

        public async Task<DirectoryInfo>Install(Progress progress)
        {
            var installer = LocalDirectoryInstaller.TryCreate(pkg, pkgDir) ??
                            HttpPackageInstaller.TryCreate(pkg, pkgDir);

            if (installer == null)
            {
                throw new InvalidOperationException($"Could not find suitable installer for package {pkg}");
            }
            var installPath = await installer.Install(progress);
            var error = await installer.Validate();
            if (error != null)
            {
                throw error;
            }
            return installPath;
        }

    }
}
