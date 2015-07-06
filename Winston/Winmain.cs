using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Winston
{
    class Winmain
    {
        static void Main(string[] args)
        {
            Task.Run(async () => await AsyncMain(args)).Wait();
        }

        static async Task AsyncMain(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            Directory.CreateDirectory(Paths.WinstonDir);

            using (var cfgProvider = new ConfigProvider())
            {
                var verb = args.First().ToLowerInvariant();
                var verbArgs = args.Skip(1);

                var repos = new Repos();
                repos.Add(@"D:\Dev\Projects\Winston\testdata\repo.txt");
                var cellar = new Cellar(Paths.WinstonDir);

                switch (verb)
                {
                    case "add":
                        await repos.InstallApps(cellar, verbArgs.ToArray());
                        break;
                    //case "remove":
                    //    cellar.Remove(verbArgs);
                    //    break;
                    //case "search":
                    //    cellar.Search(verbArgs);
                    //    break;
                    //case "list":
                    //    cellar.List(verbArgs);
                    //    break;
                    case "help":
                        Help();
                        break;
                }
            }
        }

        static void Help()
        {
        }

        static void PrintUsage()
        {
            var message = @"
To add an app, type:        winston add nameOfApp
To remove an app, type:     winston remove nameOfApp
To search for apps, type:   winston search someNameOrDescription
To list all apps, type:     winston list

For anything else, type: winston help
            
";
            Console.WriteLine(message);
        }
    }
}
