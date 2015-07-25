using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Winston
{
    static class OS
    {
        // From http://stackoverflow.com/questions/677221/copy-folders-in-c-sharp-using-system-io
        //
        static void CopyDir(string source, string destination)
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
            }

            // Process subdirectories
            var dirs = src.GetDirectories();
            foreach (var dir in dirs)
            {
                // Get destination directory
                var destinationDir = Path.Combine(dest.FullName, dir.Name);

                // Call CopyDirectory() recursively.
                CopyDir(dir.FullName, destinationDir);
            }
        }

        public static Task CopyDirectory(string source, string destination) => Task.Run(() => CopyDir(source, destination));

        public static void AddToPath(string path, string scrub = null)
        {
            using (var cu = Registry.CurrentUser)
            {
                var env = cu.OpenSubKey("Environment", true);
                var pathVar = env.GetValue("PATH", "") as string;
                var paths = ParsePaths(pathVar);

                if (scrub != null)
                {
                    // Clean any old winston entries that no longer exist.
                    // Only keep paths that are non-winston or that exist.
                    paths = paths.Where(p => !p.Contains(scrub) || Directory.Exists(p)).ToList();
                }
                if (!paths.Contains(path, Paths.NormalizedPathComparer))
                {
                    paths.Insert(0, path);
                }
                var newPath = BuildPathVar(paths);
                env.SetValue("PATH", newPath);
                BroadcastSettingsChange();
            }
        }

        public static void RemoveFromPath(string path)
        {
            using (var cu = Registry.CurrentUser)
            {
                var env = cu.OpenSubKey("Environment", true);
                var pathVar = env.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                var paths = ParsePaths(pathVar);
                var without = paths.Where(p => !Paths.NormalizedPathComparer.Equals(path, p));
                var newPath = BuildPathVar(without);
                env.SetValue("PATH", newPath);
                BroadcastSettingsChange();
            }
        }

        public static async Task<string> GetSHA1(string file) => await Task.Run(() =>
        {
            using (var f = File.OpenRead(file))
            {
                return GetSHA1(f);
            }
        });

        public static string GetSHA1(Stream stream)
        {
            var sha = new SHA1CryptoServiceProvider();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        static IList<string> ParsePaths(string pathVar)
        {
            var split = pathVar.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            return split.ToList();
        }

        static string BuildPathVar(IEnumerable<string> paths)
        {
            return string.Join(";", paths);
        }

        static void BroadcastSettingsChange()
        {
            // Send WM_SETTINGCHANGE message to all windows. Explorer will pick this up and new
            // cmd processes will see the new PATH variable.
            IntPtr lParamA = Marshal.StringToHGlobalAnsi("Environment");
            IntPtr lParamU = Marshal.StringToHGlobalUni("Environment");
            try
            {
                // Be sure to send both Unicode and ANSI messages
                IntPtr result;
                WinApi.SendMessageTimeout(
                    WinApi.HWND_BROADCAST,
                    WinApi.WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    lParamA,
                    WinApi.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
                    50,
                    out result);
                WinApi.SendMessageTimeout(
                    WinApi.HWND_BROADCAST,
                    WinApi.WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    lParamU,
                    WinApi.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
                    50,
                    out result);
            }
            finally
            {
                Marshal.FreeHGlobal(lParamA);
                Marshal.FreeHGlobal(lParamU);
            }
        }
    }
}