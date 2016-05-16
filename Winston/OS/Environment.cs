using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Winston.OS
{
    static class Environment
    {
        public static string NewLine => System.Environment.NewLine;

        public static bool IsDebug => EnvExists("WINSTON_DEBUG");

        public static void AddToPath(string path, string scrub = null)
        {
            using (var cu = Registry.CurrentUser)
            {
                var env = cu.OpenSubKey(EnvironmentKey, true);
                var pathVar = env.GetValue("PATH", "") as string;
                var paths = FileSystem.ParsePaths(pathVar);

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
                var newPath = FileSystem.BuildPathVar(paths);
                env.SetValue("PATH", newPath);
                BroadcastSettingsChange();
            }
        }

        public static void RemoveFromPath(string path)
        {
            using (var cu = Registry.CurrentUser)
            {
                var env = cu.OpenSubKey(EnvironmentKey, true);
                var pathVar = env.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
                var paths = FileSystem.ParsePaths(pathVar);
                var without =
                    paths.Where(p => !(Paths.NormalizedPathComparer.Equals(path, p) || p.ContainsInvIgnoreCase(path)));
                var newPath = FileSystem.BuildPathVar(without);
                env.SetValue("PATH", newPath);
                BroadcastSettingsChange();
            }
        }

        public static void BroadcastSettingsChange()
        {
            // Send WM_SETTINGCHANGE message to all windows. Explorer will pick this up and new
            // Cmd processes will see the new PATH variable.
            var lParamA = Marshal.StringToHGlobalAnsi(EnvironmentKey);
            var lParamU = Marshal.StringToHGlobalUni(EnvironmentKey);
            try
            {
                // Be sure to send both Unicode and ANSI messages
                IntPtr result;
                NativeMethods.SendMessageTimeout(
                    NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    lParamA,
                    NativeMethods.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
                    50,
                    out result);
                NativeMethods.SendMessageTimeout(
                    NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    lParamU,
                    NativeMethods.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG,
                    50,
                    out result);
            }
            finally
            {
                Marshal.FreeHGlobal(lParamA);
                Marshal.FreeHGlobal(lParamU);
            }
        }

        public static bool EnvExists(string environmentVarName)
        {
            return !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable(environmentVarName));
        }

#pragma warning disable CC0021 // Use nameof
        const string EnvironmentKey = "Environment";
#pragma warning restore CC0021 // Use nameof

    }
}