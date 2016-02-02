using System;
using System.Diagnostics;

namespace Winston.Test
{
    class TestProcess
    {
        readonly ProcessStartInfo info;

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public TestProcess(string path, string arguments)
        {
            info = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        public Process Run(TimeSpan timeout)
        {
            var process = new Process { StartInfo = info };
            process.Start();
            process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!process.HasExited)
            {
                process.Kill();
            }
            StdOut = process.StandardOutput.ReadToEnd();
            StdErr = process.StandardError.ReadToEnd();
            return process;
        }
    }
}