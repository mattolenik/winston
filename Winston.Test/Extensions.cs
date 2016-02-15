using System;
using System.Reflection;

namespace Winston.Test
{
    static class Extensions
    {
        public static string GetAbsolutePath(this Assembly asm)
        {
            return new Uri(asm.CodeBase).LocalPath;
        }
    }
}
