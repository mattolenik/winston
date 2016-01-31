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
        readonly string winstonExePath;

        public Winstall(string winstonExePath)
        {
            winstonHome = new TempDirectory("winston-home-test");
            this.winstonExePath = winstonExePath;
        }

        public async Task Install()
        {
            //var process = new TestProcess(winstonExePath, "selfinstall"
            await Task.Yield();
        }

        public void Dispose()
        {
            winstonHome.Dispose();
        }
    }
}