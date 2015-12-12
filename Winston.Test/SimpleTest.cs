using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSpec;

namespace Winston.Test
{
    class SimpleTest : nspec
    {
        void before_each() => Task.Run(async () =>
        {
            var path = typeof (Winmain).Assembly.Location;
            using (var install = new Winstall(path))
            {
                await Task.Delay(0);
            }
        }).Wait();

        void describe_something()
        {
            it["does a thing"] = () =>
            {
            };
        }
    }
}
