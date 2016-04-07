using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston
{
    public class Defer : IDisposable
    {
        readonly Action action;

        public Defer(Action action)
        {
            this.action = action;
        }

#pragma warning disable CC0029 // Disposables Should Call Suppress Finalize
        public void Dispose()
#pragma warning restore CC0029 // Disposables Should Call Suppress Finalize
        {
            action();
        }
    }
}
