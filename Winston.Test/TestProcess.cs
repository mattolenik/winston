using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Winston.Test
{
    class TestProcess
    {
        readonly ProcessStartInfo info;

        public TestProcess(string path, string arguments)
        {
            info = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
        }

        public async Task<Process> Run(TimeSpan timeout)
        {
            var process = new Process { StartInfo = info };
            process.Start();
            await process.WaitForExitAsync(timeout);
            if (!process.HasExited)
            {
                process.Kill();
            }
            return process;
        }
    }
}