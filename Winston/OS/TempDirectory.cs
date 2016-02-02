using System;
using System.IO;
using System.Linq;

namespace Winston.OS
{
    public sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix = "")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            Delete(Path);
        }

        void Delete(string dir)
        {
            var files = Directory.GetFiles(dir);
            var dirs = Directory.GetDirectories(dir);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var d in dirs)
            {
                Delete(d);
            }
        }

        public static implicit operator string(TempDirectory value) => value?.Path;
    }
}