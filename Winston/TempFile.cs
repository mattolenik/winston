using System;
using System.IO;
using static System.IO.Path;

namespace Winston
{
    public class TempFile : IDisposable
    {
        public string Path { get; } = GetTempFileName();

        public void Dispose() => File.Delete(Path);

        public static implicit operator string(TempFile value) => value?.Path;
    }
}