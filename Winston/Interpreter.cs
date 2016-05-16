using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winston.Cache;
using Winston.User;

namespace Winston
{
    class Interpreter
    {
        static readonly string nl = Environment.NewLine;

        public static async Task<int> RunCommandAsync(string verb, string[] verbArgs, UserProxy user, Repo repo, ConfigProvider cfg)
        {
            using (var cache = await SqliteCache.CreateAsync(cfg.GetWinstonDir(), cfg.DefaultIndex))
            {
                switch (verb)
                {
                    case "install":
                        await InstallWorkflow.InstallPackagesAsync(repo, user, cache, cfg, verbArgs);
                        return ExitCodes.Ok;

                    case "uninstall":
                        await InstallWorkflow.UninstallPackagesAsync(repo, verbArgs);
                        return ExitCodes.Ok;

                    case "search":
                    {
                        var pkgs = await cache.SearchAsync(verbArgs.First());
                        if (!pkgs.Any())
                        {
                            throw new PackageNotFoundException($"No packages found for query '{verbArgs.First()}'");
                        }
                        foreach (var pkg in pkgs)
                        {
                            Console.WriteLine(pkg.GetListing());
                        }
                        break;
                    }
                    case "list":
                        switch (verbArgs.FirstOrDefault() ?? "")
                        {
                            case "installed":
                            {
                                var pkgs = await repo.ListAsync();
                                foreach (var pkg in pkgs)
                                {
                                    Console.WriteLine(pkg.GetListing());
                                }
                                break;
                            }
                            case "available":
                            {
                                var pkgs = await cache.AllAsync();
                                foreach (var pkg in pkgs)
                                {
                                    Console.WriteLine(pkg.GetListing());
                                }
                                break;
                            }
                            case "indexes":
                            {
                                // TODO: implement list indexes
                                var indexes = await cache.GetIndexesAsync();
                                Console.WriteLine(string.Join(Environment.NewLine, indexes));
                                break;
                            }
                            default:
                                Console.WriteLine("List what? Winston can 'list' a few things:");
                                Console.WriteLine("winston list installed");
                                Console.WriteLine("winston list available");
                                Console.WriteLine("winston list indexes");
                                return ExitCodes.InvalidArgument;
                        }
                        break;
                    case "info":
                    {
                        var pkg = await cache.ByNameAsync(verbArgs.First());
                        if (pkg == null)
                        {
                            Console.WriteLine($"No package '{verbArgs.First()}' found");
                            return ExitCodes.PackageNotFound;
                        }
                        Console.WriteLine(pkg.GetInfo());
                        break;
                    }
                    case "refresh":
                        await cache.RefreshAsync();
                        return ExitCodes.Ok;

                    // TODO: finish this
                    case "restore":
                        await repo.RestoreAsync();
                        return ExitCodes.Ok;

                    case "add":
                        switch (verbArgs.First())
                        {
                            case "index":
                                var changes = await cache.AddIndexAsync(verbArgs.Skip(1).First(), forceRefresh: true);
                                if (changes.Added.Any())
                                {
                                    var msg = $"Added:{nl}{string.Join(", ", changes.Added)}";
                                    user.Message(msg);
                                }
                                if (changes.Removed.Any())
                                {
                                    var msg = $"Removed:{nl}{string.Join(", ", changes.Removed)}";
                                    user.Message(msg);
                                }
                                // TODO: implement change detection for updates
                                if (changes.Updated.Any())
                                {
                                    var msg = $"Updated:{nl}{string.Join(", ", changes.Updated)}";
                                    user.Message(msg);
                                }
                                return ExitCodes.Ok;
                            default:
                            {
                                var msg = $"Add what? Winston can 'add' one thing:{nl}winston add index <urlOrFile>";
                                user.Message(msg);
                                return ExitCodes.InvalidArgument;
                            }
                        }

                    case "help":
                        return PrintUsage();

                    default:
                        return PrintUsage();
                }
                return ExitCodes.Ok;
            }
        }

        public static int PrintUsage()
        {
            var ver = Assembly.GetExecutingAssembly().RealVersion();
            var message = $@"
To install a package:                  winston install <package>
To remove a package:                   winston uninstall <package>

To search for available packages:      winston search <nameOrDescription>
To list all available apps:            winston list available
To list installed apps:                winston list installed
To show package details:               winston info <package>

To refresh package indexes:            winston refresh

To add a package index:                winston add index <uriOrFile>
To remove a package index:             winston remove index <uriOrFile>
To list all package indexes:           winston list indexes

Show this screen:                      winston help

Winston v{ver}";
            Console.WriteLine(message);
            return ExitCodes.PrintUsage;
        }
    }
}