using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Winston
{
    public static class Paths
    {
        public static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static Uri ExecutingDir { get; }

        public static string ExecutingDirPath { get; }

        static Paths()
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            ExecutingDir = new Uri(dirPath);
            ExecutingDirPath = dirPath;
        }

        public static string AppRelative(string path)
        {
            var result = Path.Combine(ExecutingDirPath, path);
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

        public static IEnumerable<string> ResolveGlobPath(string relativeTo, string glob)
        {
            if (string.IsNullOrWhiteSpace(glob))
            {
                yield return relativeTo;
                yield break;
            }
            if(Path.IsPathRooted(glob))
            {
                throw new NotSupportedException("Rooted paths not supported");
            }
            var parts = glob.Split(new[] {"/", "\\"}, StringSplitOptions.RemoveEmptyEntries);
            if (!parts.Any())
            {
                yield return relativeTo;
                yield break;
            }
            var pattern = parts.First();
            var nextPattern = parts.Skip(1).FirstOrDefault();
            foreach (var dir in Directory.GetDirectories(relativeTo, pattern))
            {
                if (nextPattern != null)
                {
                    foreach (var match in ResolveGlobPath(dir, nextPattern))
                    {
                        yield return match;
                    }
                }
                else
                {
                    yield return dir;
                }
            }
        }
    }
}