using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Winston.Installers
{
    class ArchiveExtractor : IFileExtractor
    {
        string appDir;
        string packageFile;
        string filename;
        static readonly TimeSpan ExtractionTimeout = TimeSpan.FromMinutes(20);

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

        public static IFileExtractor TryCreate(Package pkg, string appDir, string packageFile,
            NameValueCollection headers, Uri uri)
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

        public async Task<string> Install(Progress progress)
        {
            await Extract(packageFile, appDir, progress);
            return Path.Combine(appDir, filename ?? "");
        }

        public Task<Exception> Validate()
        {
            // TODO: verify all files get extracted by comparing them to the ZIP header?
            return Task.FromResult<Exception>(null);
        }

        static async Task Extract(string filename, string destination, Progress progress)
        {
            Directory.Delete(destination, true);
            var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var args = $"x \"{filename}\" -o\"{destination}\" -y";
            var si = new ProcessStartInfo(@"tools\7z.exe", args)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            var proc = new Process { StartInfo = si };
            proc.Start();
            var exited = await proc.WaitForExitAsync(ExtractionTimeout);
            if (!exited)
            {
                proc.Kill();
                throw new TimeoutException($"Extraction of '{filename}' took longer than {ExtractionTimeout}, aborting");
            }
            if (proc.ExitCode != 0)
            {
                // TODO: better exception type
                throw new Exception(
                    $"Failed to extract archive '{filename}'. 7zip exit code: {proc.ExitCode}");
            }
            progress.CompletedInstall();
        }
    }
}