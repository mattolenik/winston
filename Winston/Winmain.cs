using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using fastJSON;
using Winston.Cache;
using Winston.Serialization;
using Winston.User;
using static Winston.InstallWorkflow;

namespace Winston
{
    class Winmain
    {
        static int Main(string[] args)
        {
            JsonConfig.Init();
            int result = -1;
            Task.Run(async () =>
            {
                //System.Diagnostics.Debugger.Launch();
                var cfg = new ConfigProvider();
                result = await AsyncMain(args, cfg);
            }).Wait();
            return result;
        }

        public static async Task<int> AsyncMain(string[] args, ConfigProvider cfg)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return ExitCodes.PrintUsage;
            }

            var verb = args.First().ToLowerInvariant();
            var verbArgs = args.Skip(1);

            if (verb == "bootstrap")
            {
                var source = verbArgs.First();
                var dest = verbArgs.Skip(1).First();
                await Bootstrap(source, dest);
                return 0;
            }
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
                            return ExitCodes.Install;
                        }
                    case "remove":
                    case "uninstall":
                        {
                            await RemoveApps(cellar, verbArgs);
                            return ExitCodes.Uninstall;
                        }
                    case "search":
                        {
                            var pkgs = await cache.Search(verbArgs.First());
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "list":
                        {
                            var pkgs = await cellar.List();
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "available":
                        {
                            var pkgs = await cache.All();
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "show":
                        {
                            var pkg = await cache.ByName(verbArgs.First());
                            Console.WriteLine(JSON.ToNiceJSON(pkg, new JSONParameters {SerializeNullValues = false}));
                            break;
                        }
                    case "refresh":
                        {
                            await cache.Refresh();
                            break;
                        }
                    case "restore":
                        {
                            await cellar.Restore();
                            return ExitCodes.Restore;
                        }
                    case "help":
                        {
                            Help();
                            break;
                        }
                    case "selfinstall":
                        {
                            await SelfInstall(cellar, verbArgs.FirstOrDefault() ?? ".");
                            break;
                        }
                    default:
                        {
                            PrintUsage();
                            return ExitCodes.PrintUsage;
                        }
                }
            }
            return 0;
        }

        static void Help()
        {
        }

        static void PrintUsage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ver = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();
            var message = $@"
To add an app:                  winston add nameOfApp
                                winston install nameOfApp

To remove an app:               winston remove nameOfApp
                                winston uninstall nameOfApp

To search for apps:             winston search someNameOrDescription
To list all available apps:     winston available
To list installed apps:         winston list

To show package details:        winston show nameOfApp

To refresh app repos:           winston refresh

For anything else:              winston help
            
Winston v{ver}";
            Console.WriteLine(message);
        }
    }
}