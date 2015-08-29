using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Winston.OS
{
    static class Environment
    {
        public static void AddToPath(string path, string scrub = null)
        {
            using (var cu = Registry.CurrentUser)
            {
                var env = cu.OpenSubKey("Environment", true);
                var pathVar = env.GetValue("PATH", "") as string;
                var paths = FS.ParsePaths(pathVar);

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
                var newPath = FS.BuildPathVar(paths);
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
                var paths = FS.ParsePaths(pathVar);
                var without =
                    paths.Where(p => !(Paths.NormalizedPathComparer.Equals(path, p) || p.ContainsInvIgnoreCase(path)));
                var newPath = FS.BuildPathVar(without);
                env.SetValue("PATH", newPath);
                BroadcastSettingsChange();
            }
        }

        public static void BroadcastSettingsChange()
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