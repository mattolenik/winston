using System;

namespace Winston.OS
{
    public interface ITempItem : IDisposable
    {
        string Path { get; }
    }
}