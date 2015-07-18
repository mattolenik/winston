using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class LocalDirectoryInstaller : IPackageInstaller
    {
        readonly string pkgDir;
        readonly string installFromDir;
        readonly string finalPath;

        public LocalDirectoryInstaller(string installFromDir, string pkgDir, string pkgFilename)
        {
            this.installFromDir = installFromDir;
            this.pkgDir = pkgDir;
            finalPath = Path.Combine(pkgDir, pkgFilename);
        }

        public async Task<string> Install()
        {
            await FS.CopyDirectory(installFromDir, pkgDir);
            return finalPath;
        }

        public Task<Exception> Validate()
        {
            return Task.FromResult(null as Exception);
        }
    }
}
