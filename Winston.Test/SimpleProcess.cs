using System;
using System.Diagnostics;

namespace Winston.Test
{
    public class SimpleProcess
    {
        readonly ProcessStartInfo info;

        public string StdOut { get; private set; }

        public string StdErr { get; private set; }

        public Process Process { get; private set; }

        public int ExitCode { get; private set; }

        public SimpleProcess(ProcessStartInfo info)
        {
            this.info = info;
        }

        public SimpleProcess(string path, string arguments)
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

        public SimpleProcess Run()
        {
            return Run(TimeSpan.FromMinutes(5));
        }

        public SimpleProcess Run(TimeSpan timeout)
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
            ExitCode = Process.ExitCode;
            return this;
        }
   }
}