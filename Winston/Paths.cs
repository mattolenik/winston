using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace Winston
{
    public static class Paths
    {
        public static string WinstonDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "winston");

        public static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(new Uri(path).LocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToLowerInvariant();
            }
            catch
            {
                // Invalid URI
                return null;
            }
        }

        class NormPathComparer : IEqualityComparer<string>
        {
            public static readonly NormPathComparer instance = new NormPathComparer();

            public bool Equals(string x, string y)
            {
                var nx = NormalizePath(x);
                var ny = NormalizePath(y);
                return string.Equals(nx, ny);
            }

            public int GetHashCode(string obj)
            {
                var n = NormalizePath(obj);
                return (n ?? obj).GetHashCode();
            }
        }

        public static IEqualityComparer<string> NormalizedPathComparer => NormPathComparer.instance;
    }
}