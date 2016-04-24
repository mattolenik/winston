using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Winston.OS
{
    public class SimpleProcess
    {
        readonly ProcessStartInfo info;

        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

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
            return Run(DefaultTimeout);
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

        public async Task<SimpleProcess> RunAsync() => await RunAsync(DefaultTimeout);

        public async Task<SimpleProcess> RunAsync(TimeSpan timeout)
        {
            Process = new Process { StartInfo = info };
            Process.Start();
            await Process.WaitForExitAsync(timeout);
            if (!Process.HasExited)
            {
                Process.Kill();
            }
            StdOut = Process.StandardOutput.ReadToEnd();
            StdErr = Process.StandardError.ReadToEnd();
            ExitCode = Process.ExitCode;
            return this;
        }

        public SimpleProcessException GetException()
        {
            return new SimpleProcessException(ExitCode, StdOut, StdErr);
        }

        public static SimpleProcess Cmd(string cmd, string workingDir = null)
        {
            return new SimpleProcess(new ProcessStartInfo
            {
                FileName = "Cmd.exe",
                Arguments = $"/c {cmd}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            });
        }
    }
}