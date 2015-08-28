using System;
using System.Collections.Generic;

namespace MergePathStrings
{
    class PathComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return string.Equals(Trim(x), Trim(y));
        }

        public int GetHashCode(string obj)
        {
            var res = Trim(obj).GetHashCode();
            return res;
        }

        string Trim(string path)
        {
            return path.ToLowerInvariant().Trim().TrimEnd('\\', '/');
        }
    }

    class Program
    {
        static void Main()
        {
            var paths = new[]
            {
                // Ordered the same way as how variables are combined and printed when
                // you type "echo %PATH%" in cmd
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
                Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process)
            };

            var result = Merge(paths);
            Console.Write(string.Join(";", result));
        }

        static IEnumerable<string> Merge(IEnumerable<string> paths)
        {
            var set = new HashSet<string>(new PathComparer());
            foreach (var path in paths)
            {
                var parts = path.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (!set.Contains(part))
                    {
                        set.Add(part);
                        yield return part;
                    }
                }
            }
        }
    }
}