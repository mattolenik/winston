using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

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
            using (var cache = new Cache(Paths.WinstonDir))
            {
                var verb = args.First().ToLowerInvariant();
                var verbArgs = args.Skip(1);

                var cellar = new Cellar(Paths.WinstonDir);
                cache.AddRepo(Path.GetFullPath(@"..\..\..\testdata\repo.txt"));
                cache.Refresh();

                switch (verb)
                {
                    case "add":
                        {
                            cellar.AddApps(cache, verbArgs.ToArray());
                            break;
                        }
                    case "remove":
                        {
                            await cellar.RemoveApps(verbArgs.ToArray());
                            break;
                        }
                    case "search":
                        {
                            var pkgs = cache.Search(verbArgs.First());
                            var serializer = new Serializer();
                            serializer.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "list":
                        {
                            var pkgs = await cellar.List();
                            var serializer = new Serializer();
                            serializer.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "refresh":
                        {
                            cache.Refresh();
                            break;
                        }
                    case "help":
                        {
                            Help();
                            return;
                        }
                    default:
                        {
                            PrintUsage();
                            return;
                        }
                }
            }
        }

        static void Help()
        {
        }

        static void PrintUsage()
        {
            var message = @"
To add an app, type:            winston add nameOfApp
To remove an app, type:         winston remove nameOfApp

To search for apps, type:       winston search someNameOrDescription
To list all apps, type:         winston list

To refresh app repos, type:     winston refresh

For anything else, type: winston help
            
";
            Console.WriteLine(message);
        }
    }
}
