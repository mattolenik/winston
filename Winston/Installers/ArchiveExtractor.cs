using RunProcess;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class ArchiveExtractor : IFileExtractor
    {
        string appDir;
        string packageFile;
        string filename;

        static readonly string[] MatchingMimeTypes =
        {
            "application/zip",
            "application/x-7z-compressed",
            "application/x-rar-compressed",
            "application/x-tar",
            "application/x-compressed",
            "application/x-gzip",
            "application/x-bzip2",
            "application/x-lzh"
        };

        static readonly string[] MatchingExtensions =
        {
            "zip", "7z", "exe", "xz", "bz2", "gz", "tar", "cab", "lzh", "lha", "rar", "xar", "z"
        };

        public static IFileExtractor TryCreate(Package pkg, string appDir, string packageFile, HttpContentHeaders headers, Uri uri)
        {
            var result = new ArchiveExtractor { appDir = appDir, packageFile = packageFile, filename = pkg.Filename };
            if (Content.ContentTypeMatches(headers, MatchingMimeTypes))
            {
                return result;
            }
            var filename = Content.MatchContentDispositionFileExt(headers, MatchingExtensions);
            if (!string.IsNullOrWhiteSpace(filename))
            {
                result.filename = filename;
                return result;
            }
            filename = Content.MatchUriFileExt(uri, MatchingExtensions);
            if (!string.IsNullOrWhiteSpace(filename))
            {
                result.filename = filename;
                return result;
            }
            return null;
        }

        public async Task<string> Install() => await Task.Run(() =>
        {
            Extract(packageFile, appDir);
            return Path.Combine(appDir, filename ?? "");
        });

        public Task<Exception> Validate()
        {
            // TODO: verify all files get extracted by comparing them to the ZIP header?
            return Task.FromResult<Exception>(null);
        }

        static void Extract(string filename, string destination)
        {
            Directory.Delete(destination, true);
            var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            using (var proc = new ProcessHost(@"tools\7z.exe", workingDir))
            {
                var args = $"x \"{filename}\" -o\"{destination}\" -y";
                proc.Start(args);

                // TODO: make config value
                int exitCode;
                if (!proc.WaitForExit(TimeSpan.FromMinutes(10), out exitCode))
                {
                    proc.Kill();
                    throw new TimeoutException("Took too long to extract archive: " + filename);
                }
                var stdout = proc.StdOut.ReadAllText(Encoding.UTF8);
                var stderr = proc.StdErr.ReadAllText(Encoding.UTF8);
                if (exitCode != 0)
                {
                    // TODO: better exception type
                    throw new Exception(
                        $"Failed to extract archive '{filename}'. 7zip stdout:\n{stdout}\n\n7zip stderr:{stderr}\n");
                }
            }
        }
    }
}
