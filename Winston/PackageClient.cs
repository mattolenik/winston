﻿using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Winston.Installers;

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

        public async Task<DirectoryInfo> Install()
        {
            var installer = LocalDirectoryInstaller.TryCreate(pkg, pkgDir) ??
                            HttpPackageInstaller.TryCreate(pkg, pkgDir);

            var installPath = await installer.Install();
            var error = await installer.Validate();
            if (error != null)
            {
                throw error;
            }
            return installPath;
        }

    }
}
