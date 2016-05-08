using System;

namespace Winston.OS
{
    public interface IDisposablePathItem : IDisposable
    {
        string Path { get; }
    }
}