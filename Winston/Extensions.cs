using System;

namespace Winston
{
    static class Extensions
    {
        public static string Fmt(this string format, IFormatProvider provider, params object[] args)
        {
            return string.Format(provider, format, args);
        }

        public static string Fmt(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        public static bool EqualsOrdIgnoreCase(this string str, string other)
        {
            return string.Equals(str, other, StringComparison.OrdinalIgnoreCase);
        }
    }
}
