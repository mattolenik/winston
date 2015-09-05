using System;
using System.Diagnostics;
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
    }
}