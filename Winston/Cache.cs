using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Winston
{
    public class Cache : IDisposable
    {
        readonly string sourcesPath;
        readonly string cachePath;

        // TODO: concurrent collection unneeded? reexamine
        ConcurrentDictionary<string, List<Package>> cache;

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
            // TODO: use binary serialization instead
            // TODO: fix improper default values
            Yml.TryLoad(sourcesPath, out sources, () => new HashSet<string>(StringComparer.InvariantCultureIgnoreCase));
            Yml.TryLoad(cachePath, out cache, () => new ConcurrentDictionary<string, List<Package>>(StringComparer.InvariantCultureIgnoreCase));
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
            // TODO: non-crash handling of missing or failed repos
            if (!LocalFileRepo.CanLoad(uriOrPath)) throw new Exception("Can't load repo " + uriOrPath);

            Repo r;
            if (!LocalFileRepo.TryLoad(uriOrPath, out r))
            {
                throw new Exception($"Unable to load repo with URI: {uriOrPath}");
            }
            foreach (var pkg in r.Packages)
            {
                if (!cache.ContainsKey(pkg.Name))
                {
                    cache[pkg.Name] = new List<Package>();
                }
                cache[pkg.Name].Add(pkg);
            }
        }

        public async Task Refresh()
        {
            cache.Clear();
            // TODO: repos with colliding app names will clobber each other here
            var tasks = sources.Select(s => Task.Run(() => Put(s)));
            await Task.WhenAll(tasks);
        }

        public IEnumerable<Package> ByName(string pkgName)
        {
            // TODO: Handle unknown package case. Use option types
            List<Package> matches;
            cache.TryGetValue(pkgName, out matches);
            return matches;
        }

        public IEnumerable<Package> Search(string query)
        {
            query = query.ToLowerInvariant();
            if (cache.ContainsKey(query))
            {
                foreach (var match in cache[query])
                {
                    yield return match;
                }
                yield break;
            }
            foreach (var pkg in cache.Values.SelectMany(p=>p))
            {
                if (pkg.Name.Contains(query) || pkg.Description.Contains(query))
                {
                    yield return pkg;
                }
            }
        }

        public IEnumerable<Package> All => cache.Values.SelectMany(p => p);

        public void Dispose() => Save();

        public bool Empty() => !sources.Any();
    }

    // TODO: find a better way to do this
    static class LocalFileRepo
    {
        public static bool CanLoad(string uriOrPath)
        {
            Uri p;
            return Uri.TryCreate(uriOrPath, UriKind.RelativeOrAbsolute, out p);
        }

        public static bool TryLoad(string uriOrPath, out Repo repo)
        {
            try
            {
                Uri path;
                if (!Uri.TryCreate(uriOrPath, UriKind.RelativeOrAbsolute, out path))
                {
                    repo = null;
                    return false;
                }
                var resolvedPath = path.IsAbsoluteUri ? path.LocalPath : uriOrPath;
                if (File.Exists(resolvedPath))
                {
                    repo = JsonConvert.DeserializeObject<Repo>(File.ReadAllText(resolvedPath));
                    repo.URL = uriOrPath;
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
