using System;

namespace Winston.Test.Debug
{
    class Program
    {
        static void Main(string[] args)
        {
            new DebuggerShim().NSpec_Tests();
        }
    }
}
