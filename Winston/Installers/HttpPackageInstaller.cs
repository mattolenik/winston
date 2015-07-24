﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class HttpPackageInstaller : IPackageInstaller
    {
        readonly Package pkg;
        readonly string pkgDir;
        readonly TempFile tmpFile;

        public static IPackageInstaller TryCreate(Package pkg, string pkgDir)
        {
            if (pkg.URL.Scheme == "http" || pkg.URL.Scheme == "https")
            {
                return new HttpPackageInstaller(pkg, pkgDir);
            }
            return null;
        }

        HttpPackageInstaller(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
            tmpFile = new TempFile();
        }

        public async Task<DirectoryInfo> Install()
        {
            using (var client = new HttpClient())
            using (var res = await client.GetAsync(pkg.URL))
            using (var body = await res.Content.ReadAsStreamAsync())
            {
                using (var file = File.OpenWrite(tmpFile))
                {
                    await body.CopyToAsync(file);
                }

                var hash = await OS.GetSHA1(tmpFile);
                // Only check when SHA1 is specified in the package metadata
                if (!string.IsNullOrWhiteSpace(pkg.SHA1) &&
                    !string.Equals(hash, pkg.SHA1, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"SHA1 hash of remote file {pkg.URL} did not match {pkg.SHA1}");
                }
                var version = pkg.ResolveVersion() ?? hash;

                // Save package information to disk first. Other actions can use this
                // to interact with a package without having to load whole repos into memory.
                Directory.CreateDirectory(pkgDir);
                Yml.Save(pkg, Path.Combine(pkgDir, "pkg.yml"));

                // TODO: replace hash with version resolution
                var installDir = Path.Combine(pkgDir, version);
                Directory.CreateDirectory(installDir);

                var archive = ArchiveExtractor.TryCreate(pkg, installDir, tmpFile, res.Content.Headers, pkg.URL) ??
                              ExeExtractor.TryCreate(pkg, installDir, tmpFile, res.Content.Headers, pkg.URL);
                if (archive != null)
                {
                    var p = await archive.Install();
                }
                else
                {
                    throw new NotSupportedException("Unable to identify type of package at URL: " + pkg.URL);
                }

                return new DirectoryInfo(installDir);
            }
        }

        public Task<Exception> Validate()
        {
            return Task.FromResult(null as Exception);
        }

        public void Dispose() => tmpFile?.Dispose();
    }
}