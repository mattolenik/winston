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
        public TempDirectory WinstonHome { get; }
        readonly string sourceDir;

        public Winstall(string winstonSourceDir)
        {
            WinstonHome = TempDirectory.New("winston-test");
            sourceDir = winstonSourceDir;
        }

        public int Bootstrap(TimeSpan timeout)
        {
            var winstonExe = Path.Combine(sourceDir, "winston.exe");
            var process = new SimpleProcess(winstonExe, $"bootstrap \"{WinstonHome.Path}\"");
            process.Run(timeout);
            Console.WriteLine(process.StdOut);
            Console.WriteLine(process.StdErr);
            return process.Process.ExitCode;
        }

        public void Dispose()
        {
            WinstonHome.Dispose();
        }
    }
}