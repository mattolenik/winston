using System;
using System.IO;
using System.Text;

namespace Winston.OS
{
    public sealed class TempFile : IDisposable
    {
        static readonly Encoding utf8bomless = new UTF8Encoding(false);

        public TempFile(string extension = null)
        {
            extension = extension?.ToLowerInvariant()?.Trim('.');
            var path = System.IO.Path.GetTempFileName();
            if (extension != null)
            {
                var newPath = $"{path}.{extension}";
                File.Move(path, newPath);
                path = newPath;
            }
            Path = path;
        }

        public string Path { get; }

        public void Dispose() => File.Delete(Path);

        public static implicit operator string(TempFile value) => value?.Path;

        public void WriteAllText(string text, Encoding encoding = null)
        {
            File.WriteAllText(Path, text, encoding ?? utf8bomless);
        }
    }
}