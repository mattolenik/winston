using System;

namespace Winston
{
    public sealed class Defer : IDisposable
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
