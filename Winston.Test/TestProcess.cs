using System;
using System.Diagnostics;

namespace Winston.Test
{
    class TestProcess
    {
        readonly ProcessStartInfo info;

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public Process Process { get; private set; }

        TestProcess(ProcessStartInfo info)
        {
            this.info = info;
        }

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

        public static TestProcess Shell(string cmd)
        {
            return new TestProcess(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{cmd}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
        }

        public void Run(TimeSpan timeout)
        {
            Process = new Process { StartInfo = info };
            Process.Start();
            Process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!Process.HasExited)
            {
                Process.Kill();
            }
            StdOut = Process.StandardOutput.ReadToEnd();
            StdErr = Process.StandardError.ReadToEnd();
        }
    }
}