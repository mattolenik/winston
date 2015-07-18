using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
