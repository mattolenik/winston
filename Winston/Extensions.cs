using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Winston
{
    static class Extensions
    {
        public static bool EqualsOrdIgnoreCase(this string str, string other) => string.Equals(str, other, StringComparison.OrdinalIgnoreCase);

        public static bool EqualsInvIgnoreCase(this string str, string other)
            => string.Equals(str, other, StringComparison.InvariantCultureIgnoreCase);

        public static bool ContainsInvIgnoreCase(this string str, string other)
        {
            return str != null && str.ToLowerInvariant().Contains(other.ToLowerInvariant());
        }

        public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
        {
            return await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));
        }

        public static string LastSegment(this Uri uri)
        {
            return uri?.Segments.Last();
        }

        public static string RealVersion(this Assembly asm)
        {
            return AssemblyName.GetAssemblyName(asm.Location).Version.ToString();
        }

        public static string Directory(this Assembly asm)
        {
            return Path.GetDirectoryName(new Uri(asm.CodeBase).LocalPath);
        }

        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool Like(this string str, string pattern)
        {
            return new Regex(
                "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(str);
        }
    }
}