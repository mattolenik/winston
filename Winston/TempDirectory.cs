using System;
using System.IO;

namespace Winston
{
    public class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix = "")
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void Dispose() => Directory.Delete(Path, true);

        public static implicit operator string(TempDirectory value) => value?.Path;
    }
}