using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Winston
{
    public static class Paths
    {
        public static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string ExecutingDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);

        public static string AppRelative(string path)
        {
            var result = Path.Combine(ExecutingDir, path);
            return result;
        }

        public static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToLowerInvariant();
            }
#pragma warning disable CC0003
            catch
            {
                // Invalid URI
                return null;
            }
#pragma warning restore CC0003
        }

        class NormPathComparer : IEqualityComparer<string>
        {
            public static readonly NormPathComparer instance = new NormPathComparer();

            public bool Equals(string x, string y)
            {
                var nx = NormalizePath(x);
                var ny = NormalizePath(y);
                return String.Equals(nx, ny);
            }

            public int GetHashCode(string obj)
            {
                var n = NormalizePath(obj);
                return (n ?? obj).GetHashCode();
            }
        }

        public static IEqualityComparer<string> NormalizedPathComparer => NormPathComparer.instance;

        public static string GetDirectory(string path)
        {
            if (path == null) return null;
            return Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        }

        public static string GetRelativePath(string from, string to)
        {
            var path1 = new Uri(from);
            var path2 = new Uri(to);
            var diff = path1.MakeRelativeUri(path2);
            return diff.OriginalString.Replace('/', '\\');
        }
    }
}