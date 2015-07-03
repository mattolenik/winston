using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

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
    }
}
