using System.IO;

namespace Winston.OS
{
    public sealed class TempDirectory : ITempItem
    {
        public string Path { get; }

        public TempDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            Delete(Path);
            Directory.Delete(Path, true);
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
                var fi = new FileInfo(d);
                if (fi.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    JunctionPoint.Delete(d);
                }
                else
                {
                    Delete(d);
                }
            }
        }

        public static implicit operator string(TempDirectory value) => value?.Path;

        public override string ToString()
        {
            return Path;
        }
    }
}