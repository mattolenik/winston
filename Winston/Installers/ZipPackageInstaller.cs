using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class ZipPackageInstaller : IPackageInstaller
    {
        string appDir;
        string packageFile;
        string filename;

        public static ZipPackageInstaller TryCreate(Package pkg, string appDir, string packageFile, HttpContentHeaders headers, Uri uri)
        {
            var result = new ZipPackageInstaller { appDir = appDir, packageFile = packageFile, filename = pkg.Filename };
            if (Content.ContentTypeMatches(headers, "application/zip"))
            {
                return result;
            }
            var filename = Content.MatchContentDispositionFileExt(headers, "zip");
            if (!string.IsNullOrWhiteSpace(filename))
            {
                result.filename = filename;
                return result;
            }
            filename = Content.MatchUriFileExt(uri, "zip");
            if (!string.IsNullOrWhiteSpace(filename))
            {
                result.filename = filename;
                return result;
            }
            return null;
        }

        public async Task<string> Install()
        {
            return await Task.Run(() =>
            {
                using (var f = File.OpenRead(packageFile))
                {
                    Unzip(f, appDir);
                    return Path.Combine(appDir, filename);
                }
            });
        }

        public Task<Exception> Validate()
        {
            // TODO: verify all files get extracted by comparing them to the ZIP header?
            return Task.FromResult<Exception>(null);
        }

        static void Unzip(Stream stream, string destination)
        {
            Directory.Delete(destination, true);
            using (var zip = new ZipArchive(stream))
            {
                zip.ExtractToDirectory(destination);
            }
        }
    }
}
