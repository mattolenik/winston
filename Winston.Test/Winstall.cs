using System;
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

        public int Bootstrap()
        {
            var process = new TestProcess(sourceDir, $"bootstrap \"{sourceDir}\" \"{winstonHome.Path}\"");
            var result = process.Run(TimeSpan.FromSeconds(10));
            return result.ExitCode;
        }

        public void Dispose()
        {
            winstonHome.Dispose();
        }
    }
}