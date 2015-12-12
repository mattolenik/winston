using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Winston.OS;

namespace Winston.Test
{
    /// <summary>
    /// A Winston install for testing.
    /// </summary>
    public class Winstall : IDisposable
    {
        readonly TempDirectory winstonHome;
        readonly string winstonAppExePath;

        public Winstall(string winstonAppExePath)
        {
            winstonHome = new TempDirectory("winston-home-test");
            this.winstonAppExePath = winstonAppExePath;
        }

        public async Task Install()
        {
            //var process = new TestProcess(winstonAppExePath, "selfinstall"
            await Task.Yield();
        }

        public void Dispose()
        {
            winstonHome.Dispose();
        }
    }
}