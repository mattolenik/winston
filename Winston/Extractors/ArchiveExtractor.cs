using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winston.Fetchers;

namespace Winston.Extractors
{
    public class ArchiveExtractor : IPackageExtractor
    {
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

        public bool IsMatch(TempPackage package)
        {
            return MatchingMimeTypes.Any(t => t.Equals(package.MimeType, StringComparison.InvariantCultureIgnoreCase)) ||
                   MatchingExtensions.Any(
                       ext => package.FileName?.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase) ?? false);
        }

        public async Task ExtractAsync(TempPackage package, string destination, Progress progress)
        {
            await ExtractAsync(package.PackageItem.Path, destination, progress);
        }

        public static async Task<Exception> ValidateAsync()
        {
            // TODO: verify all files get extracted by comparing them to the ZIP header?
            return await Task.FromResult<Exception>(null);
        }

        static async Task ExtractAsync(string filename, string destination, Progress progress)
        {
            Directory.Delete(destination, true);
            var workingDir = Paths.ExecutingDirPath;
            var args = $"x \"{filename}\" -o\"{destination}\" -y";
            var si = new ProcessStartInfo(@"tools\7z.exe", args)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            using (var proc = new Process { StartInfo = si })
            {
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
}