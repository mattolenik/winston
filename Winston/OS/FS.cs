using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Winston.OS
{
    static class FS
    {
        // From http://stackoverflow.com/questions/677221/copy-folders-in-c-sharp-using-system-io
        //
        static void CopyDir(string source, string destination, Progress progress, int total, ref int current)
        {
            var src = new DirectoryInfo(source);
            var dest = new DirectoryInfo(destination);

            if (!dest.Exists)
            {
                dest.Create();
            }

            // Copy all files
            var files = src.GetFiles();
            foreach (var file in files)
            {
                file.CopyTo(Path.Combine(dest.FullName, file.Name), true);
                current++;
                progress.UpdateInstall((int)Math.Round((double)current / total * 100.0));
            }

            // Process subdirectories
            var dirs = src.GetDirectories();
            foreach (var dir in dirs)
            {
                // Get destination directory
                var destinationDir = Path.Combine(dest.FullName, dir.Name);

                // Call CopyDirectory() recursively.
                CopyDir(dir.FullName, destinationDir, progress, total, ref current);
            }
        }

        public static Task CopyDirectoryAsync(string source, string destination, Progress progress) => Task.Run(() =>
        {
            progress.UpdateInstall(0);
            var total = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Count();
            var current = 0;
            CopyDir(source, destination, progress, total, ref current);
        });

        public static async Task<string> GetSHA1Async(string file) => await Task.Run(() =>
        {
            using (var f = File.OpenRead(file))
            {
                return GetSHA1(f);
            }
        });

        public static string GetSHA1(Stream stream)
        {
            using (var sha = new SHA1CryptoServiceProvider())
            {
                var hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        internal static IList<string> ParsePaths(string pathVar)
        {
            var split = pathVar.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            return split.ToList();
        }

        internal static string BuildPathVar(IEnumerable<string> paths)
        {
            return string.Join(";", paths);
        }
    }
}