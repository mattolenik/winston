﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Winston.Cache;
using Winston.Serialization;
using Winston.User;
using static Winston.InstallWorkflow;
using Environment = Winston.OS.Environment;

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
            try
            {
                if (args.Length < 1)
                {
                    return Interpreter.PrintUsage();
                }

                var verb = args.First().ToLowerInvariant();
                var verbArgs = args.Skip(1).Select(a => a.ToLowerInvariant()).ToArray();

                if (verb == "bootstrap")
                {
                    var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
                    var source = Paths.GetDirectory(Uri.UnescapeDataString(uri.AbsolutePath));
                    var dest = verbArgs.First();
                    await BootstrapAsync(source, dest);
                    return 0;
                }
                Directory.CreateDirectory(cfg.ResolvedWinstonDir);

                using (var user = new UserProxy(new ConsoleUserAdapter(Console.Out, Console.In)))
                using (var cache = await SqliteCache.CreateAsync(cfg.ResolvedWinstonDir))
                {
                    var repo = new Repo(user, cfg.ResolvedWinstonDir);
                    // TODO: find a better way to setup repos
                    // Set up default repo
                    if (verb != "selfinstall" && cache.Empty())
                    {
                        await cache.AddRepoAsync(Paths.AppRelative(@"repos\default.json"));
                        await cache.RefreshAsync();
                    }

                    return await Interpreter.RunCommandAsync(verb, verbArgs, user, repo, cache);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAILED");
                Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                // TODO: log details somewhere?
                if (Environment.IsDebug)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return ExitCodes.Exception;
            }
        }
    }
}