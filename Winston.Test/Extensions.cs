using System;
using System.Reflection;
using fastJSON;

namespace Winston.Test
{
    static class Extensions
    {
        public static string GetAbsolutePath(this Assembly asm)
        {
            return new Uri(asm.CodeBase).LocalPath;
        }

        public static string ToJSON(this object obj)
        {
            return JSON.ToNiceJSON(obj, new JSONParameters { EnableAnonymousTypes = true });
        }
    }
}
