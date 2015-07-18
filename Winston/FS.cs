﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Winston
{
    static class FS
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

        public static void UpdatePath(string path)
        {
            using (var cu = Registry.CurrentUser)
            {
                path = path.Trim().Trim('\\', '/', '.');
                var env = cu.OpenSubKey("Environment", true);
                var pathVar = env.GetValue("PATH", "") as string;
                if (pathVar.ContainsInvIgnoreCase(path))
                {
                    return;
                }
                var newPath = $"{path};{pathVar}";
                env.SetValue("PATH", newPath);
            }
        }
    }
}