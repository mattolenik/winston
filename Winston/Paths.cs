using System;
using System.IO;

namespace Winston
{
    public static class Paths
    {
        public static string WinstonDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "winston");
    }
}