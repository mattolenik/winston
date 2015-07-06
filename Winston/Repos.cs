using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Winston
{
    public class Repos
    {
        readonly HashSet<Repo> repos = new HashSet<Repo>();

        public void Add(string uriOrPath)
        {
            if (LocalFileRepo.CanLoad(uriOrPath))
            {
                Repo r;
                if (LocalFileRepo.TryLoad(uriOrPath, out r))
                {
                    repos.Add(r);
                    r.Url = uriOrPath;
                    return;
                }
                throw new Exception("Unable to load repo with URI: {0}".Fmt(uriOrPath));
            }
        }

        public async Task InstallApps(Cellar cellar, params string[] apps)
        {
            await Task.WhenAll(apps.Select(async appName =>
            {
                var app = repos.SelectMany(r => r.Packages).Single(p => string.Equals(appName, p.Name, StringComparison.OrdinalIgnoreCase));
                await cellar.Add(app);
            }));
        }
    }

    static class LocalFileRepo
    {
        public static bool CanLoad(string uriOrPath)
        {
            Uri p;
            return Uri.TryCreate(uriOrPath, UriKind.Absolute, out p);
        }

        public static bool TryLoad(string uriOrPath, out Repo repo)
        {
            try
            {
                Uri path;
                if (!Uri.TryCreate(uriOrPath, UriKind.Absolute, out path))
                {
                    repo = null;
                    return false;
                }
                if (File.Exists(path.LocalPath))
                {
                    repo = JsonConvert.DeserializeObject<Repo>(File.ReadAllText(path.LocalPath));
                    repo.Url = uriOrPath;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to refresh repo");
                Console.Error.WriteLine(e);
            }
            repo = null;
            return false;
        }
    }
}
