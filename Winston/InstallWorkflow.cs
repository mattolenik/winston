using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston
{
    static class InstallWorkflow
    {
        public static async Task AddApps(Cellar cellar, QuestionQueue queue, Cache cache, params string[] appNames) => await AddApps(cellar, queue, cache, appNames as IEnumerable<string>);

        public static async Task AddApps(Cellar cellar, QuestionQueue queue, Cache cache, IEnumerable<string> appNames) => await Task.Run(() =>
        {
            var pkgs = appNames.Select(cache.ByName);
            var pkgsList = pkgs as List<IEnumerable<Package>> ?? pkgs.ToList();
            var unique = pkgsList.Where(p => p.Count() == 1).SelectMany(p => p);
            var ambiguous = pkgsList.Where(p => p.Count() > 1);
            var choiceTasks = ambiguous.Select(async choices => await Disambiguate(queue, choices));
            var chosen = Task.WhenAll(choiceTasks).Result;
            Task.WaitAll(unique.Union(chosen).Select(async p => await cellar.Add(p)).ToArray());
        });

        public static async Task RemoveApps(Cellar cellar, IEnumerable<string> apps) => await RemoveApps(cellar, apps.ToArray());

        public static async Task RemoveApps(Cellar cellar, params string[] apps)
        {
            await Task.WhenAll(apps.Select(async appName => await cellar.Remove(appName)));
        }

        // TODO: abstract away from text/console
        static async Task<Package> Disambiguate(QuestionQueue queue, IEnumerable<Package> choices)
        {
            var sb = new StringBuilder();
            var msg = "\nMultiple packages found, please select which to install:\n\n";
            sb.Append(msg);

            var choicesArray = choices as Package[] ?? choices.ToArray();
            var diffProps = Reflect.Diff(choicesArray).ToArray();
            for(int i = 1; i <= choicesArray.Length; i++)
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
            var answer = await queue.Ask(q);
            var ansInt = int.Parse(answer) - 1;
            return choicesArray[ansInt];
        }

        public static async Task SelfInstall(Cellar cellar, string installFromDir)
        {
            var fullDir = Path.GetFullPath(installFromDir);
            var ver = FileVersionInfo.GetVersionInfo(Path.Combine(fullDir, "winston.exe"));
            var pkg = new Package
            {
                Name = ver.ProductName, // "Winston"
                Description = ver.Comments, // "Winston app manager."
                URL = fullDir,
                Filename = "winston.exe",
                Type = PackageType.Shell,
                Version = ver.FileVersion // "0.1.0.0"
            };
            await cellar.Add(pkg);
            OS.UpdatePath(cellar.BinPath);
        }
    }
}