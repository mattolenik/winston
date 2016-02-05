using System;
using System.IO;
using Winston.OS;

namespace Winston.Test
{
    /// <summary>
    /// A Winston install for testing.
    /// </summary>
    public class Winstall : IDisposable
    {
        readonly TempDirectory winstonHome;
        readonly string sourceDir;

        public Winstall(string winstonSourceDir)
        {
            winstonHome = new TempDirectory("winston-home-test");
            sourceDir = winstonSourceDir;
        }

        public int Bootstrap(TimeSpan timeout)
        {
            var winstonExe = Path.Combine(sourceDir, "winston.exe");
            var process = new TestProcess(winstonExe, $"bootstrap \"{winstonHome.Path}\"");
            var result = process.Run(timeout);
            Console.WriteLine(process.StdOut);
            Console.WriteLine(process.StdErr);
            return result.ExitCode;
        }

        public void Dispose()
        {
            winstonHome.Dispose();
        }
    }
}