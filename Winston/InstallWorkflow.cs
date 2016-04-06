using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Winston.Cache;
using Winston.Packaging;
using Winston.Serialization;
using Winston.User;

namespace Winston
{
    static class InstallWorkflow
    {
        public static async Task InstallPackagesAsync(Repo repo, UserProxy user, SqliteCache cache, ConfigProvider cfg, IEnumerable<string> appNames)
            => await InstallPackagesAsync(repo, user, cache, cfg, appNames.ToArray());

        public static async Task InstallPackagesAsync(Repo repo, UserProxy user, SqliteCache cache, ConfigProvider cfg, params string[] appNames)
        {
            var pkgs = await cache.ByNamesAsync(appNames);
            var pkgsList = pkgs as List<Package> ?? pkgs.ToList();
            if (!pkgsList.Any())
            {
                user.Message($"No packages found matching {string.Join(", ", appNames)}");
                throw new PackageNotFoundException();
            }
            var unique = pkgsList.Where(p => p.Variants.Count == 0);
            var ambiguous = pkgsList.Where(p => p.Variants.Count > 0);
            var choiceTasks = ambiguous.Select(async choices => await DisambiguateAsync(user, SelectPlatform(choices)));
            var chosen = Task.WhenAll(choiceTasks).Result;
            // TODO: break this up
            var ap =
                unique.Union(chosen).Select(async p => await repo.AddAsync(p, inject: true, writeRegistryPath: cfg.WriteRegistryPath));
            await Task.WhenAll(ap);
        }

        public static async Task UninstallPackagesAsync(Repo repo, IEnumerable<string> apps) => await UninstallPackagesAsync(repo, apps.ToArray());

        public static async Task UninstallPackagesAsync(Repo repo, params string[] apps)
        {
            await Task.WhenAll(apps.Select(async appName => await repo.RemoveAsync(appName)));
        }

        static IEnumerable<Package> SelectPlatform(Package pkg)
        {
            var platform = Environment.Is64BitProcess ? Platform.x64 : Platform.x86;
            return pkg.Variants.Where(p => p.Platform == platform || p.Platform == Platform.Any).Select(p => p.Merge(pkg));
        }

        // TODO: abstract away from text/console
        static async Task<Package> DisambiguateAsync(UserProxy queue, IEnumerable<Package> choices)
        {
            var first = choices.FirstOrDefault();
            if (first != null) return first;

            var sb = new StringBuilder();
            const string msg = "\nMultiple packages found, please select which to install:\n\n";
            sb.Append(msg);

            var choicesArray = choices as Package[] ?? choices.ToArray();
            var diffProps = Reflect.Diff(choicesArray).ToArray();
            for (int i = 1; i <= choicesArray.Length; i++)
            {
                var c = choicesArray[i - 1];
                sb.AppendLine($"{i}. {c.Name}");
                foreach (var p in diffProps)
                {
                    var v = p.GetValue(c);
                    sb.AppendLine($"{p.Name}: {v}");
                }
                sb.AppendLine();
            }

            var preamble = sb.ToString();

            // TODO: make Question generic to avoid conversion
            var chs = Enumerable.Range(1, choicesArray.Length).Select(x => x.ToString());
            var q = new Question(preamble, "Which package number?", chs);
            var answer = await queue.AskAsync(q);
            var ansInt = int.Parse(answer) - 1;
            return choicesArray[ansInt];
        }

        public static async Task<string> SelfInstallAsync(Repo repo, string installFromDir)
        {
            var fullDir = Path.GetFullPath(installFromDir);
            var winstonExe = Path.Combine(fullDir, "winston.exe");
            var ver = FileVersionInfo.GetVersionInfo(winstonExe);
            var asmVersion = Assembly.LoadFile(winstonExe).GetName().Version.ToString();
            var pkg = new Package
            {
                Name = ver.ProductName,
                Description = ver.Comments,
                Location = new Uri(fullDir),
                Type = PackageType.Shell,
                Version = asmVersion,
            };
            return await repo.AddAsync(pkg);
        }

        public static async Task<string> BootstrapAsync(string installSource, string destination)
        {
            var installSourceFull = Path.GetFullPath(installSource);
            var winstonExe = Path.Combine(installSourceFull, "winston.exe");
            var ver = FileVersionInfo.GetVersionInfo(winstonExe);
            var asmVersion = Assembly.LoadFile(winstonExe).GetName().Version.ToString();
            var pkg = new Package
            {
                Name = ver.ProductName,
                Description = ver.Comments,
                Location = new Uri(installSourceFull),
                Type = PackageType.Shell,
                Version = asmVersion
            };
            var repo = new Repo(new UserProxy(new HeadlessUserAdapter()), destination);
            var pkgDir = await repo.AddAsync(pkg: pkg, inject: true, writeRegistryPath: false);
            var cfg = new Config
            {
                // This resolves to {destination}, relative to {pkgDir}
                WinstonDir = "../../../",
                WriteRegistryPath = false
            };
            Directory.CreateDirectory(pkgDir);
            var cfgFile = Path.Combine(pkgDir, "config.yml");
            Yml.Save(cfg, cfgFile);
            return pkgDir;
        }
    }
}