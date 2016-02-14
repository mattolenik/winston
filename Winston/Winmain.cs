using System;
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
            var result = -1;
            Task.Run(async () =>
            {
                var cfg = new ConfigProvider();
                result = await MainAsync(args, cfg);
            }).Wait();
            return result;
        }

        public static async Task<int> MainAsync(string[] args, ConfigProvider cfg)
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
                var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
                var source = Paths.GetDirectory(Uri.UnescapeDataString(uri.AbsolutePath));
                var dest = verbArgs.First();
                await BootstrapAsync(source, dest);
                return 0;
            }
            Directory.CreateDirectory(cfg.Config.WinstonDir);

            using (var user = new UserProxy(new ConsoleUserAdapter(Console.Out, Console.In)))
            using (var cache = await SqliteCache.CreateAsync(cfg.Config.WinstonDir))
            {
                var cellar = new Cellar(user, cfg.Config.WinstonDir);
                // TODO: find a better way to setup repos
                // Set up default repo
                if (verb != "selfinstall" && cache.Empty())
                {
                    await cache.AddRepoAsync(Paths.AppRelative(@"repos\default.json"));
                    await cache.RefreshAsync();
                }

                switch (verb)
                {
                    case "add":
                    case "install":
                        {
                            await AddAppsAsync(cellar, user, cache, verbArgs);
                            return ExitCodes.Install;
                        }
                    case "remove":
                    case "uninstall":
                        {
                            await RemoveAppsAsync(cellar, verbArgs);
                            return ExitCodes.Uninstall;
                        }
                    case "search":
                        {
                            var pkgs = await cache.SearchAsync(verbArgs.First());
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "list":
                        {
                            var pkgs = await cellar.ListAsync();
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "available":
                        {
                            var pkgs = await cache.AllAsync();
                            foreach (var pkg in pkgs)
                            {
                                Console.WriteLine(pkg.ToString());
                            }
                            break;
                        }
                    case "show":
                        {
                            var pkg = await cache.ByNameAsync(verbArgs.First());
                            Console.WriteLine(JSON.ToNiceJSON(pkg, new JSONParameters {SerializeNullValues = false}));
                            break;
                        }
                    case "refresh":
                        {
                            await cache.RefreshAsync();
                            break;
                        }
                    case "restore":
                        {
                            await cellar.RestoreAsync();
                            return ExitCodes.Restore;
                        }
                    case "help":
                        {
                            Help();
                            break;
                        }
                    case "selfinstall":
                        {
                            await SelfInstallAsync(cellar, verbArgs.FirstOrDefault() ?? ".");
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