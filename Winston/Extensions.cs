using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    }
}