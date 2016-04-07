using System;
using System.IO;
using System.Text;

namespace Winston.OS
{
    public sealed class TempFile : IDisposable
    {
        static readonly Encoding utf8bomless = new UTF8Encoding(false);

        public string Path { get; } = System.IO.Path.GetTempFileName();

        public void Dispose() => File.Delete(Path);

        public static implicit operator string(TempFile value) => value?.Path;

        public void WriteAllText(string text, Encoding encoding = null)
        {
            File.WriteAllText(Path, text, encoding ?? utf8bomless);
        }
    }
}