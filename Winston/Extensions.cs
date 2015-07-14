using System;

namespace Winston
{
    static class Extensions
    {
        public static bool EqualsOrdIgnoreCase(this string str, string other) => string.Equals(str, other, StringComparison.OrdinalIgnoreCase);
    }
}