using System;
using System.IO;

namespace Winston
{
    public static class Paths
    {
        public static string WinstonDir
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "winston");
            }
        }
    }
}