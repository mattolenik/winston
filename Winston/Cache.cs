using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Winston
{
    public class Cache : IDisposable
    {
        readonly string sourcesPath;
        readonly string cachePath;

        Dictionary<string, Package> cache;

        // TODO: worry about concurrent access here?
        HashSet<string> sources;

        public Cache(string cellarPath)
        {
            sourcesPath = Path.Combine(cellarPath, "sources.yml");
            cachePath = Path.Combine(cellarPath, "cache.yml");
            Load();
        }

        void Load()
        {
            Yml.TryLoad(sourcesPath, out sources, () => new HashSet<string>(StringComparer.InvariantCultureIgnoreCase));
            Yml.TryLoad(cachePath, out cache, () => new Dictionary<string, Package>(StringComparer.InvariantCultureIgnoreCase));
        }

        void Save()
        {
            Yml.Save(sources, sourcesPath);
            Yml.Save(cache, cachePath);
        }

        public void AddRepo(string uriOrPath)
        {
            sources.Add(uriOrPath);
        }

        void Put(string uriOrPath)
        {
            if (!LocalFileRepo.CanLoad(uriOrPath)) throw new Exception("Can't load repo " + uriOrPath);

            Repo r;
            if (!LocalFileRepo.TryLoad(uriOrPath, out r))
            {
                throw new Exception("Unable to load repo with URI: {0}".Fmt(uriOrPath));
            }
            foreach (var pkg in r.Packages)
            {
                cache[pkg.Name] = pkg;
            }
        }


        public void Reload()
        {
            foreach (var source in sources)
            {
                Put(source);
            }
        }

        public Package ByName(string pkgName)
        {
            return cache[pkgName];
        }

        public void Dispose()
        {
            Save();
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
