using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using static Winston.InstallWorkflow;

namespace Winston
{
    class Winmain
    {
        static readonly Serializer serializer = new Serializer();

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
            using (var queue = new QuestionQueue())
            {
                var verb = args.First().ToLowerInvariant();
                var verbArgs = args.Skip(1);

                var cellar = new Cellar(Paths.WinstonDir);
                cache.AddRepo(Path.GetFullPath(@"..\..\..\testdata\repo.json"));
                await cache.Refresh();

                switch (verb)
                {
                    case "add":
                        {
                            await AddApps(cellar, queue, cache, verbArgs);
                            break;
                        }
                    case "remove":
                        {
                            await RemoveApps(cellar, verbArgs);
                            break;
                        }
                    case "search":
                        {
                            var pkgs = cache.Search(verbArgs.First());
                            serializer.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "list":
                        {
                            var pkgs = await cellar.List();
                            serializer.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "available":
                        {
                            var pkgs = cache.All;
                            serializer.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "refresh":
                        {
                            await cache.Refresh();
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
To add an app:                  winston add nameOfApp
To remove an app:               winston remove nameOfApp

To search for apps:             winston search someNameOrDescription
To list all available apps:     winston available
To list installed apps:         winston list

To refresh app repos:           winston refresh

For anything else:              winston help
            
";
            Console.WriteLine(message);
        }
    }
}
