using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class ExeExtractor : IFileExtractor
    {
        string packageFile;
        string exePath;

        public static IFileExtractor TryCreate(Package pkg, string appDir, string packageFile, HttpContentHeaders headers, Uri uri)
        {
            var result = new ExeExtractor { packageFile = packageFile };
            var cdFilename = Content.MatchContentDispositionFileExt(headers, "exe");
            var uriFilename = Content.MatchUriFileExt(uri, "exe");
            var pkgFilename = Path.GetExtension(pkg.Filename).EqualsOrdIgnoreCase("exe") ? pkg.Filename : null;
            var filename = pkgFilename ?? cdFilename ?? uriFilename;
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }
            result.exePath = Path.Combine(appDir, filename);
            return result;
        }

        public async Task<string> Install()
        {
            return await Task.Run(() =>
            {
                File.Delete(exePath);
                File.Move(packageFile, exePath);
                return exePath;
            });
        }

        public async Task<Exception> Validate()
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(exePath))
                {
                    return new FileNotFoundException("EXE was expected to exist but does not", exePath);
                }
                return null;
            });
        }
    }
}
