using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Winston.Packaging;

namespace Winston.Installers
{
    class ExeExtractor : IPackageExtractor
    {
        string packageFile;
        string exePath;

        public static IPackageExtractor TryCreate(Package pkg, string installDir, string packageFile, NameValueCollection headers, Uri uri)
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
            result.exePath = Path.Combine(installDir, filename);
            return result;
        }

        public async Task<string> InstallAsync(Progress progress)
        {
            return await Task.Run(() =>
            {
                File.Delete(exePath);
                File.Move(packageFile, exePath);
                return exePath;
            });
        }

        public async Task<Exception> ValidateAsync()
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
