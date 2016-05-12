using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Winston.MSBuildTasks
{
    public class EmbedBin : Task
    {
        [Required]
        public string SourceFile { get; set; }

        [Required]
        public string OutputH { get; set; }

        [Required]
        public string OutputCpp { get; set; }

        public override bool Execute()
        {
            source = new FileInfo(SourceFile);
            sourceName = source.Name.Replace('.', '_');
            WriteCpp();
            WriteH();
            return true;
        }

        FileInfo source;
        string sourceName;

        void WriteCpp()
        {
            using (var cppFile = new StreamWriter(File.Open(OutputCpp, FileMode.Create, FileAccess.ReadWrite), Encoding.UTF8))
            using (var file = File.OpenRead(SourceFile))
            {
                cppFile.WriteLine($"unsigned char {sourceName}[] = {{"); // Double { is escaping for string interp
                var b = 0;
                var column = 0;
                var columnMax = 120;
                while (b >= 0)
                {
                    b = file.ReadByte();
                    if (b < 0)
                    {
                        break;
                    }
                    var bStr = b.ToString();
                    cppFile.Write(bStr);
                    cppFile.Write(',');
                    column += bStr.Length + 1;
                    if (column > columnMax)
                    {
                        cppFile.WriteLine();
                        column = 0;
                    }
                }
                cppFile.WriteLine("};");
            }
        }

        void WriteH()
        {
            var text = $@"#pragma once
extern unsigned char {sourceName}[];
extern const size_t {sourceName}_length = {source.Length};
";
            File.WriteAllText(OutputH, text, Encoding.UTF8);
        }
    }
}