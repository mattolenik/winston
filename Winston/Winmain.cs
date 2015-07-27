using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winston.Cache;
using Winston.Serialization;
using Winston.User;
using static Winston.InstallWorkflow;

namespace Winston
{
    class Winmain
    {
        static void Main(string[] args) => Task.Run(async () =>
        {
            var cfg = new ConfigProvider();
            await AsyncMain(args, cfg);
        }).Wait();

        public static async Task AsyncMain(string[] args, ConfigProvider cfg)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            var verb = args.First().ToLowerInvariant();
            var verbArgs = args.Skip(1);

            Directory.CreateDirectory(cfg.Config.WinstonDir);

            using (var user = new UserProxy(new ConsoleUserAdapter(Console.Out, Console.In)))
            using (var cache = await SqliteCache.Create(cfg.Config.WinstonDir))
            {
                var cellar = new Cellar(user, cfg.Config.WinstonDir);
                // TODO: find a better way to setup repos
                // Set up default repo
                if (verb != "selfinstall" && cache.Empty())
                {
                    await cache.AddRepo(Paths.AppRelative(@"repos\default.json"));
                    await cache.Refresh();
                }

                switch (verb)
                {
                    case "add":
                    case "install":
                        {
                            await AddApps(cellar, user, cache, verbArgs);
                            break;
                        }
                    case "remove":
                    case "uninstall":
                        {
                            await RemoveApps(cellar, verbArgs);
                            break;
                        }
                    case "search":
                        {
                            var pkgs = await cache.Search(verbArgs.First());
                            if (pkgs.Any())
                            {
                                Yml.Serialize(Console.Out, pkgs);
                            }
                            break;
                        }
                    case "list":
                        {
                            var pkgs = await cellar.List();
                            Yml.Serialize(Console.Out, pkgs);
                            break;
                        }
                    case "available":
                        {
                            var pkgs = await cache.All();
                            Yml.Serialize(Console.Out, pkgs);
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
                    case "selfinstall":
                        {
                            await SelfInstall(cellar, verbArgs.FirstOrDefault() ?? ".");
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
            var assembly = Assembly.GetExecutingAssembly();
            var ver = FileVersionInfo.GetVersionInfo(assembly.Location);
            var message = $@"
To add an app:                  winston add nameOfApp
                                winston install nameOfApp

To remove an app:               winston remove nameOfApp
                                winston uninstall nameOfApp

To search for apps:             winston search someNameOrDescription
To list all available apps:     winston available
To list installed apps:         winston list

To refresh app repos:           winston refresh

For anything else:              winston help
            
Winston v{ver.ProductVersion}";
            Console.WriteLine(message);
        }
    }
}