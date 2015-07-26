﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winston.Serialization;
using YamlDotNet.Serialization;
using static Winston.InstallWorkflow;

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

            var verb = args.First().ToLowerInvariant();
            var verbArgs = args.Skip(1);
            Directory.CreateDirectory(Paths.WinstonDir);

            using (var cfgProvider = new ConfigProvider())
            using (var user = new UserProxy())
            using (var cache = await Cache.Create(Paths.WinstonDir))
            {
                var cellar = new Cellar(user, Paths.WinstonDir);
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
                        {
                            await AddApps(cellar, user, cache, verbArgs);
                            break;
                        }
                    case "remove":
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
To remove an app:               winston remove nameOfApp

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