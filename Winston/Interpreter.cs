using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using fastJSON;
using Winston.Cache;
using Winston.User;

namespace Winston
{
    class Interpreter
    {
        public static async Task<int> RunCommandAsync(string verb, string[] verbArgs, UserProxy user, Repo repo, SqliteCache cache)
        {
            switch (verb)
            {
                case "install":
                    await InstallWorkflow.InstallPackagesAsync(repo, user, cache, verbArgs);
                    return ExitCodes.OK;

                case "uninstall":
                    await InstallWorkflow.UninstallPackagesAsync(repo, verbArgs);
                    return ExitCodes.OK;

                case "search":
                    {
                        var pkgs = await cache.SearchAsync(verbArgs.First());
                        foreach (var pkg in pkgs)
                        {
                            Console.WriteLine(pkg.GetListing());
                        }
                        break;
                    }
                case "list":
                    switch (verbArgs.First())
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
                                break;
                            }
                        default:
                            Console.WriteLine("List what? Winston can 'list' a few things:");
                            Console.WriteLine("winston list installed");
                            Console.WriteLine("winston list available");
                            return ExitCodes.InvalidArgument;
                    }
                    break;
                case "info":
                    {
                        var pkg = await cache.ByNameAsync(verbArgs.First());
                        Console.WriteLine(JSON.ToNiceJSON(pkg, new JSONParameters { SerializeNullValues = false }));
                        break;
                    }
                case "refresh":
                    await cache.RefreshAsync();
                    break;

                // TODO: finish this
                case "restore":
                    await repo.RestoreAsync();
                    return ExitCodes.OK;

                case "add":
                    switch (verbArgs.First())
                    {
                        case "index":
                            await cache.AddRepoAsync(verbArgs.Skip(1).First());
                            break;
                        default:
                            Console.WriteLine("Add what? Winston can only 'add' one thing:");
                            Console.WriteLine("winston add index <urlOrFile>");
                            return ExitCodes.InvalidArgument;
                    }
                    break;

                case "help":
                    return PrintUsage();

                case "selfinstall":
                    await InstallWorkflow.SelfInstallAsync(repo, verbArgs.FirstOrDefault() ?? ".");
                    break;

                default:
                    return PrintUsage();
            }
            return 0;
        }

        public static int PrintUsage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ver = AssemblyName.GetAssemblyName(assembly.Location).Version.ToString();
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