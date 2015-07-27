using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Winston
{
    public class Cache : IDisposable
    {
        readonly string dbPath;
        internal SQLiteConnection DB { get; private set; }

        Cache(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public static async Task<Cache> Create(string winstonDir)
        {
            var dbPath = Path.Combine(winstonDir, "cache.sqlite");
            var cache = new Cache(dbPath);
            await cache.Load();
            return cache;
        }

        async Task<bool> TryCreateTables() => await Task.Run(() =>
        {
            using (var t = DB.BeginTransaction())
            {
                try
                {
                    var result = DB.Execute(@"
    CREATE TABLE IF NOT EXISTS `Packages` (
	`Name`	TEXT NOT NULL,
	`Description`	TEXT,
	`PackageData`	TEXT NOT NULL,
    PRIMARY KEY(Name)
)", transaction: t);
                    result = DB.Execute(@"
    CREATE TABLE IF NOT EXISTS `Sources` (
	`URI`	TEXT NOT NULL,
	PRIMARY KEY(URI)
)", transaction: t);
                    result = DB.Execute(@"CREATE INDEX IF NOT EXISTS `PackageIndex` ON `Packages` (`Name` ASC)", transaction: t);
                    result = DB.Execute(@"CREATE VIRTUAL TABLE IF NOT EXISTS `PackageSearch` USING fts3(Name, Desc)", transaction: t);

                    t.Commit();
                }
                catch
                {
                    t.Rollback();
                    return false;
                }
            }
            return true;
        });

        async Task Load()
        {
            DB = new SQLiteConnection($"Data Source={dbPath}");
            await DB.OpenAsync();
            var canContinue = await TryCreateTables();
            if (!canContinue)
            {
                // If the expected schema can't be created, delete
                // the cache file and start over.
                DB.Dispose();
                File.Delete(dbPath);
                DB = new SQLiteConnection($"Data Source={dbPath}");
                await DB.OpenAsync();
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
            var result = DB.Execute("insert or replace into Sources (URI) values (@URI)", new { URI = uriOrPath });
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
            using (var t = DB.BeginTransaction())
            {
                try
                {
                    foreach (var pkg in r.Packages)
                    {
                        var json = JsonConvert.SerializeObject(
                            pkg,
                            Formatting.None,
                            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        var result = DB.Execute(
                            @"insert or replace into Packages (Name, Description, PackageData) values (@Name, @Desc, @Data)",
                            new {Name = pkg.Name, Desc = pkg.Description, Data = json},
                            transaction: t);
                        result = DB.Execute(
                            @"insert or replace into PackageSearch (Name, Desc) values (@Name, @Desc)",
                            new {Name = pkg.Name, Desc = pkg.Description},
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
            var repos = await DB.QueryAsync<string>("select URI from Sources");
            foreach (var repo in repos)
            {
                await LoadRepo(repo);
            }
        }

        public async Task<Package> ByName(string pkgName)
        {
            var result = await DB.QueryAsync<string>("select PackageData from Packages where Name = @Name", new { Name = pkgName });
            var json = result.Single();
            var pkg = JsonConvert.DeserializeObject<Package>(json);
            return pkg;
        }

        public async Task<IList<Package>> ByNames(IEnumerable<string> names)
        {
            return await Task.WhenAll(names.Select(ByName));
        }

        public async Task<IList<Package>> Search(string query)
        {
            var nameMatches = await DB.QueryAsync<string>("select distinct Name from PackageSearch where Name match @query", new {query});
            var descMatches = await DB.QueryAsync<string>("select distinct Name from PackageSearch where Desc match @query", new {query});
            var names = nameMatches.Union(descMatches).Distinct();
            var result = new List<Package>();
            foreach (var name in names)
            {
                var json = await DB.QueryAsync<string>("select PackageData from Packages where Name = @name", new {name});
                var pkg = JsonConvert.DeserializeObject<Package>(json.Single());
                result.Add(pkg);
            }

            return result;
        }

        public async Task<IEnumerable<Package>> All()
        {
            var result = await DB.QueryAsync<string>("select PackageData from Packages");
            var pkgs = result.Select(JsonConvert.DeserializeObject<Package>);
            return pkgs;
        }

        public void Dispose() => DB?.Dispose();

        public bool Empty()
        {
            var result = DB.Query<int>("select count(1) from Sources").Single();
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
