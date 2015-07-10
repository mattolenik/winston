using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Winston.Installers;

namespace Winston
{
    public class PackageClient : IDisposable
    {
        readonly Package pkg;
        readonly string pkgDir;
        readonly TempFile tmpFile;

        public PackageClient(Package pkg, string pkgDir)
        {
            this.pkg = pkg;
            this.pkgDir = pkgDir;
            tmpFile = new TempFile();
        }

        async Task<IPackageInstaller> Get()
        {
            using (var client = new HttpClient())
            using (var res = await client.GetAsync(pkg.URL))
            using (var body = await res.Content.ReadAsStreamAsync())
            {
                using (var file = File.OpenWrite(tmpFile))
                {
                    await body.CopyToAsync(file);
                }
                var hash = ValidateSHA1(pkg.SHA1, tmpFile);

                // Save package information to disk first. Other actions can use this
                // to interact with a package without having to load whole repos into memory.
                Directory.CreateDirectory(pkgDir);
                Yml.Save(pkg, Path.Combine(pkgDir, "pkg.yml"));

                // TODO: replace hash with version resolution
                var appDir = Path.Combine(pkgDir, hash);
                Directory.CreateDirectory(appDir);

                var uri = new Uri(pkg.URL);

                var archive = ArchivePackageInstaller.TryCreate(pkg, appDir, tmpFile, res.Content.Headers, uri);
                if (archive != null) return archive;

                var exe = ExePackageInstaller.TryCreate(pkg, appDir, tmpFile, res.Content.Headers, uri);
                if (exe != null) return exe;

                throw new NotSupportedException("Unable to identify type of package at URL: " + pkg.URL);
            }
        }

        public async Task<string> Install()
        {
            var installer = await Get();
            var installPath = await installer.Install();
            var error = await installer.Validate();
            if (error != null)
            {
                throw error;
            }
            return installPath;
        }

        static string ValidateSHA1(string sha1, string file)
        {
            string hash;
            using (var f = File.OpenRead(file))
            {
                hash = GetSha1(f);
            }

            if (!string.IsNullOrWhiteSpace(sha1) &&
                !string.Equals(sha1, hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Hash of remote file {0} did not match expected {1}".Fmt(hash, sha1));
            }
            return hash;
        }

        static string GetSha1(Stream stream)
        {
            var sha = new SHA1CryptoServiceProvider();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            if (tmpFile != null)
            {
                tmpFile.Dispose();
            }
        }
    }
}
