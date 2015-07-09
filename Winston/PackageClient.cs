using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading.Tasks;

namespace Winston
{
    public class PackageClient
    {
        public PackageClient()
        {
        }

        public async Task<string> Install(Package pkg, string pkgDir)
        {
            using (var tmpFile = new TempFile())
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
                var installPath = Path.Combine(pkgDir, hash);
                Directory.CreateDirectory(installPath);

                var handler = GetHandler(res.Content.Headers, pkg.URL);
                await handler(tmpFile, installPath);


                return installPath;
            }
        }

        static Func<string, string, Task> GetHandler(HttpContentHeaders headers, string url)
        {
            // We'll consider MIME type to be the most authoritative answer on what kind of
            // package this file is.
            var contentType = headers.GetValues("Content-Type").SingleOrDefault() ?? "";
            switch (contentType.ToLowerInvariant())
            {
                case "application/zip":
                    return HandleZIP;
                case "application/x-ms-dos-program":
                    return HandleEXE;
            }

            // Followed by checking file extension in Content-Disposition filename header
            var disposition = headers.GetValues("Content-Disposition").SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(disposition))
            {
                var cd = new ContentDisposition(disposition);
                var filename = cd.FileName;
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    var ext = Path.GetExtension(filename).ToLowerInvariant();
                    switch (ext)
                    {
                        case "zip":
                            return HandleZIP;
                        case "exe":
                            return HandleEXE;
                    }
                }
            }

            // Followed by checking file extension in the URL
            var uri = new Uri(url);
            if (uri.IsFile)
            {
                var localPath = uri.LocalPath;
                var ext = Path.GetExtension(localPath).ToLowerInvariant();
                switch (ext)
                {
                    case "zip":
                        return HandleZIP;
                    case "exe":
                        return HandleEXE;
                }
            }

            throw new NotSupportedException("Package at URL is not a supported format: " + url);
        }

        static async Task HandleEXE(string file, string installPath)
        {
            // TODO: implement exe handling
            throw new NotImplementedException();
        }

        static async Task HandleZIP(string file, string installPath)
        {
            await Task.Run(() =>
            {
                using (var f = File.OpenRead(file))
                {
                    Unzip(f, installPath);
                }
            });
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

        static void Unzip(Stream stream, string destination)
        {
            Directory.Delete(destination, true);
            using (var zip = new ZipArchive(stream))
            {
                zip.ExtractToDirectory(destination);
            }
        }

        static string GetSha1(Stream stream)
        {
            var sha = new SHA1CryptoServiceProvider();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
