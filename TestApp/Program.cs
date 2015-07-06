using System;
using System.IO;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(string.Join(",", args));
            Console.WriteLine(Directory.GetCurrentDirectory());
            var input = Console.ReadLine();
            Console.Error.WriteLine(input);
        }
    }
}