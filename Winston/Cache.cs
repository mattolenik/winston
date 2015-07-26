using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Winston.Serialization;

namespace Winston
{
    public class Cache : IDisposable
    {
        readonly string dbPath;
        SQLiteConnection db;

        Cache(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public static async Task<Cache> Create(string cellarPath)
        {
            var dbPath = Path.Combine(cellarPath, "cache.sqlite");
            var db = new Cache(dbPath);
            await db.Load();
            return db;
        }

        async Task<bool> TryCreateTables()
        {
            using (var t = db.BeginTransaction())
            {
                try
                {
                    var result = await db.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS `Packages` (
	`Name`	TEXT NOT NULL,
	`Description`	TEXT,
	`PackageData`	TEXT NOT NULL,
    PRIMARY KEY(Name)
)");
                    result = await db.ExecuteAsync(@"
    CREATE TABLE IF NOT EXISTS `Sources` (
	`URI`	TEXT NOT NULL,
	PRIMARY KEY(URI)
)");
                    result = await db.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS `PackageIndex` ON `Packages` (`Name` ASC)");
                    t.Commit();
                }
                catch
                {
                    t.Rollback();
                    return false;
                }
            }
            return true;
        }

        async Task Load()
        {
            db = new SQLiteConnection($"Data Source={dbPath}");
            await db.OpenAsync();
            var canContinue = await TryCreateTables();
            if (!canContinue)
            {
                // If the expected schema can't be created, delete
                // the cache file and start over.
                db.Dispose();
                File.Delete(dbPath);
                db = new SQLiteConnection($"Data Source={dbPath}");
                await db.OpenAsync();
                // If table creation still fails, throw
                canContinue = await TryCreateTables();
                if (!canContinue)
                {
                    throw new Exception("Unable to create cache database");
                }
            }
        }

        public async Task AddRepo(string uriOrPath)
        {
            uriOrPath = new Uri(uriOrPath).AbsoluteUri;
            var result = db.Execute("insert or replace into Sources (URI) values (@URI)", new { URI = uriOrPath });
            await LoadRepo(uriOrPath);
        }

        async Task LoadRepo(string uriOrPath) => await Task.Run(() =>
        {
            // TODO: non-crash handling of missing or failed repos
            if (!LocalFileRepo.CanLoad(uriOrPath)) throw new Exception("Can't load repo " + uriOrPath);

            Repo r;
            if (!LocalFileRepo.TryLoad(uriOrPath, out r))
            {
                throw new Exception($"Unable to load repo with URI: {uriOrPath}");
            }
            using (var t = db.BeginTransaction())
            {
                try
                {
                    foreach (var pkg in r.Packages)
                    {
                        var json = JsonConvert.SerializeObject(
                            pkg,
                            Formatting.None,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        var result = db.Execute(
                            @"insert or replace into Packages (Name, Description, PackageData) values (@Name, @Desc, @Data)",
                            new {Name = pkg.Name, Desc = pkg.Description, Data = json},
                            transaction: t);
                    }
                    t.Commit();
                }
                catch
                {
                    t.Rollback();
                    throw;
                }
            }
        });

        public async Task Refresh()
        {
            var repos = await db.QueryAsync<string>("select URI from Sources");
            foreach (var repo in repos)
            {
                await LoadRepo(repo);
            }
        }

        public async Task<Package> ByName(string pkgName)
        {
            var result = await db.QueryAsync<string>("select PackageData from Packages where Name = @Name", new { Name = pkgName });
            var json = result.Single();
            var pkg = JsonConvert.DeserializeObject<Package>(json);
            return pkg;
        }

        public async Task<IEnumerable<Package>> ByNames(IEnumerable<string> names)
        {
            return await Task.WhenAll(names.Select(ByName));
        }

        public IEnumerable<Package> Search(string query)
        {
            return null;
        }

        public async Task<IEnumerable<Package>> All()
        {
            var result = await db.QueryAsync<string>("select PackageData from Packages");
            var pkgs = result.Select(JsonConvert.DeserializeObject<Package>);
            return pkgs;
        }

        public void Dispose() => db?.Dispose();

        public bool Empty()
        {
            var result = db.Query<int>("select count(1) from Sources").Single();
            return result == 0;
        }
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
