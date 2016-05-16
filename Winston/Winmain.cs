using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

                using (var user = new UserProxy(new ConsoleUserAdapter(Console.Out, Console.In)))
                {
                    // Handle installation verbs
                    switch (verb)
                    {
                        case "selfinstall":
                            await SelfInstallAsync(user, verbArgs.FirstOrDefault() ?? ".");
                            return 0;

                        case "bootstrap":
                            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
                            var source = Paths.GetDirectory(Uri.UnescapeDataString(uri.AbsolutePath));
                            var dest = verbArgs.First();
                            await BootstrapAsync(source, dest);
                            return 0;
                    }
                    var winstonDir = cfg.GetWinstonDir();
                    Directory.CreateDirectory(winstonDir);

                    UniqifyWinstonDir(winstonDir);
                    var repo = new Repo(user, winstonDir);
                    return await Interpreter.RunCommandAsync(verb, verbArgs, user, repo, cfg);
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
                return (ex as IExitCodeException)?.ErrorCode ?? ExitCodes.Exception;
            }
        }

        /// <summary>
        /// Places a unique GUID so that this directory can always be identified as belonging to Winston.
        /// This makes operations like cleaning and repair easier (just look for the uniqifer file)
        /// </summary>
        /// <param name="dir"></param>
        static void UniqifyWinstonDir(string dir)
        {
            var asm = Assembly.GetExecutingAssembly();
            var attrs = asm.GetCustomAttributes<AssemblyMetadataAttribute>();
            var uniqifier = attrs?.FirstOrDefault(a => a.Key == "uniq")?.Value;
            if (uniqifier == null)
            {
                throw new Exception("No uniqifier found in assmebly metadata");
            }
            File.WriteAllText(Path.Combine(dir, "uniq"), uniqifier);
        }
    }
}