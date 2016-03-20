using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WinstonTasks
{
    public class TarFile : Task
    {
        [Required]
        public string Name { get; set; }

        public string[] Files { get; set; }

        public string Directory { get; set; }

        public override bool Execute()
        {
            if (Files != null && Files.Length > 0)
            {
                var files = Files.Select(f => new {Absolute = Path.GetFullPath(f), ShortName = Path.GetFileName(f)});
                using (var tarOutput = new TarOutputStream(File.Create(Name)))
                {
                    foreach (var file in files)
                    {
                        using (var fileStream = File.OpenRead(file.Absolute))
                        {
                            var entry = TarEntry.CreateTarEntry(file.ShortName);
                            entry.Size = fileStream.Length;
                            tarOutput.PutNextEntry(entry);
                            fileStream.CopyTo(tarOutput);
                            tarOutput.CloseEntry();
                        }
                    }
                }
                return true;
            }

            var dir = Path.GetFullPath(Directory);
            if (System.IO.Directory.Exists(dir))
            {
                using (var tarOutput = new TarOutputStream(File.Create(Name)))
                {
                    AddDirectory(tarOutput, dir, dir);
                }
                return true;
            }
            return false;
        }

        static void AddDirectory(TarOutputStream tar, string baseDirectory, string directory)
        {
            if (baseDirectory != directory)
            {
                var dirEntry = TarEntry.CreateTarEntry(GetRelative(directory, baseDirectory));
                dirEntry.TarHeader.TypeFlag = TarHeader.LF_DIR;
                dirEntry.TarHeader.Mode = 1003; // magic number from TarEntry.cs
                tar.PutNextEntry(dirEntry);
            }
            var dirInfo = new DirectoryInfo(directory);
            foreach (var dir in dirInfo.GetDirectories())
            {
                AddDirectory(tar, baseDirectory, dir.FullName);
            }
            foreach (var file in dirInfo.GetFiles())
            {
                using (var fileStream = File.OpenRead(file.FullName))
                {
                    var rel = GetRelative(file.FullName, baseDirectory);
                    var entry = TarEntry.CreateTarEntry(rel);
                    entry.Size = fileStream.Length;
                    tar.PutNextEntry(entry);
                    fileStream.CopyTo(tar);
                    tar.CloseEntry();
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