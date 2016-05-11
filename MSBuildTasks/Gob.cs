using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Winston.MSBuildTasks
{
    /// <summary>
    /// An ad hoc format like tar, but simpler
    /// </summary>
    public class Gob : Task
    {
        [Required]
        public string OutputFile { get; set; }

        [Required]
        public string SourceDirectory { get; set; }

        public override bool Execute()
        {
            var outDir = Path.GetDirectoryName(OutputFile);
            System.IO.Directory.CreateDirectory(outDir);
            var dir = Path.GetFullPath(SourceDirectory);
            if (System.IO.Directory.Exists(dir))
            {
                using (var gob = new FileStream(OutputFile, FileMode.Create))
                {
                    AddDirectory(gob, dir, dir);
                }
                return true;
            }
            return false;
        }

        static void AddDirectory(Stream gob, string baseDirectory, string directory)
        {
            var dirName = GetRelative(directory, baseDirectory);
            // If dirName is empty, we are at the root
            if (dirName != string.Empty)
            {
                var header = new GobHeader(dirName, GobHeaderType.Directory);
                header.CopyTo(gob);
            }
            var dirInfo = new DirectoryInfo(directory);
            foreach (var dir in dirInfo.GetDirectories())
            {
                AddDirectory(gob, baseDirectory, dir.FullName);
            }
            foreach (var file in dirInfo.GetFiles())
            {
                var rel = GetRelative(file.FullName, baseDirectory);
                var header = new GobHeader(rel, GobHeaderType.File, file.Length);
                header.CopyTo(gob);
                if (file.Length > 0)
                {
                    using (var fileStream = File.OpenRead(file.FullName))
                    {
                        fileStream.CopyTo(gob);
                    }
                }
            }
        }

        static string GetRelative(string file, string relativeTo)
        {
            var fileAbs = new Uri(file, UriKind.Absolute);
            var relRoot = new Uri(Path.GetFullPath(relativeTo), UriKind.Absolute);
            return relRoot.MakeRelativeUri(fileAbs).ToString();
        }
    }
}