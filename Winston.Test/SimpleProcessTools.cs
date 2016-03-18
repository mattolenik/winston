using System;
using System.Diagnostics;

namespace Winston.Test
{
    public class SimpleProcessTools
    {
        public static SimpleProcess cmd(string cmd, string workingDir = null)
        {
            return new SimpleProcess(new ProcessStartInfo
            {
                FileName = "cmd.exe",
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
