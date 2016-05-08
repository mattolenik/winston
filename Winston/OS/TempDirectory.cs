using System.IO;

namespace Winston.OS
{
    public sealed class TempDirectory : ITempItem
    {
        public string Path { get; }

        TempDirectory(string path)
        {
            Path = path;
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

        public static TempDirectory New(string prefix = "")
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public static TempDirectory FromExisting(string directory)
        {
            return new TempDirectory(directory);
        }
    }
}