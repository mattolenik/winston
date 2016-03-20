using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace WinstonTasks
{
    public class TarTask : Task
    {
        [Required]
        public string TarFileName { get; set; }

        [Required]
        public string[] Files { get; set; }

        public string RelativeTo { get; set; }

        public override bool Execute()
        {
            var files = Files.Select(f => new { Absolute = Path.GetFullPath(f), Name = Path.GetFileName(f) });
            using (var tarOutput = new TarOutputStream(File.Create(TarFileName)))
            {
                foreach (var file in files)
                {
                    using (var fileStream = File.OpenRead(file.Absolute))
                    {
                        var name = !string.IsNullOrWhiteSpace(RelativeTo) ? GetRelative(file.Absolute, RelativeTo) : file.Name;
                        var entry = TarEntry.CreateTarEntry(name);
                        entry.Size = fileStream.Length;
                        tarOutput.PutNextEntry(entry);
                        fileStream.CopyTo(tarOutput);
                        tarOutput.CloseEntry();
                    }
                }
            }
            return true;
        }

        static string GetRelative(string file, string relativeTo)
        {
            var fileAbs = new Uri(file, UriKind.Absolute);
            var relRoot = new Uri(Path.GetFullPath(relativeTo), UriKind.Absolute);
            return relRoot.MakeRelativeUri(fileAbs).ToString();
        }
    }
}