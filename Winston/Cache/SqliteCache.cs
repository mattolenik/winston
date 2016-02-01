using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fastJSON;

namespace Winston.Cache
{
    public sealed class SqliteCache : IDisposable
    {
        readonly string dbPath;
        internal SQLiteConnection Db { get; private set; }

        SqliteCache(string dbPath)
        {
            this.dbPath = dbPath;
            SQLiteConnection.CreateFile(dbPath);
        }

        public static async Task<SqliteCache> Create(string winstonDir)
        {
            var dbPath = Path.Combine(winstonDir, "cache.sqlite");
            var cache = new SqliteCache(dbPath);
            await cache.Load();
            return cache;
        }

        async Task<bool> TryCreateTables() => await Task.Run(() =>
        {
            using (var t = Db.BeginTransaction())
            {
                try
                {
                    // TODO: check results?
                    var result = Db.Execute(Tables.Packages.CreateStatement, transaction: t);
                    result = Db.Execute(Tables.Sources.CreateStatement, transaction: t);
                    result = Db.Execute(Indexes.PackageIndex.CreateStatement, transaction: t);
                    result = Db.Execute(Tables.PackageSearch.CreateStatement, transaction: t);
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
            Db = new SQLiteConnection($"Data Source={dbPath}");
            await Db.OpenAsync();
            var canContinue = await TryCreateTables();
            if (!canContinue)
            {
                // If the expected schema can't be created, delete
                // the cache file and start over.
                Db.Dispose();
                File.Delete(dbPath);
                SQLiteConnection.CreateFile(dbPath);
                Db = new SQLiteConnection($"Data Source={dbPath}");
                await Db.OpenAsync();
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
            var result = Db.Execute("insert or replace into Sources (URI) values (@URI)", new { URI = uriOrPath });
            await LoadRepo(uriOrPath);
        }

        async Task LoadRepo(string uriOrPath) => await Task.Run(() =>
        {
            // TODO: non-crash handling of missing or failed repos
            if (!LocalFileRepo.CanLoad(uriOrPath))
            {
                throw new Exception("Can't load repo " + uriOrPath);
            }

            Repo r;
            if (!LocalFileRepo.TryLoad(uriOrPath, out r))
            {
                throw new Exception($"Unable to load repo with URI: {uriOrPath}");
            }
            using (var t = Db.BeginTransaction())
            {
                try
                {
                    foreach (var pkg in r.Packages)
                    {
                        var json = JSON.ToJSON(pkg, new JSONParameters {SerializeNullValues = false});
                        var result = Db.Execute(
                            @"insert or replace into Packages (Name, Description, PackageData) values (@Name, @Desc, @Data)",
                            new { Name = pkg.Name, Desc = pkg.Description, Data = json },
                            transaction: t);
                        result = Db.Execute(
                            @"insert or replace into PackageSearch (Name, Desc) values (@Name, @Desc)",
                            new { Name = pkg.Name, Desc = pkg.Description },
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
            var repos = await Db.QueryAsync<string>("select URI from Sources");
            foreach (var repo in repos)
            {
                await LoadRepo(repo);
            }
        }

        public async Task<Package> ByName(string pkgName)
        {
            var result = await Db.QueryAsync<string>("select PackageData from Packages where Name = @Name", new { Name = pkgName });
            var json = result.SingleOrDefault();
            if (json == null) return null;
            var pkg = JSON.ToObject<Package>(json);
            return pkg;
        }

        public async Task<IList<Package>> ByNames(IEnumerable<string> names)
        {
            // SQLite is going to end up executing these all in serial anyway,
            // little point in making this code parallel.
            var result = new List<Package>();
            foreach (var name in names)
            {
                var match = await ByName(name);
                if (match != null)
                {
                    result.Add(match);
                }
            }
            return result;
        }

        public async Task<IList<Package>> Search(string query)
        {
            var nameMatches = await Db.QueryAsync<string>("select distinct Name from PackageSearch where Name match @query", new { query });
            var descMatches = await Db.QueryAsync<string>("select distinct Name from PackageSearch where Desc match @query", new { query });
            var names = nameMatches.Union(descMatches).Distinct();
            var result = new List<Package>();
            foreach (var name in names)
            {
                var json =
                    await Db.QueryAsync<string>("select PackageData from Packages where Name = @name", new { name });
                var single = json.SingleOrDefault();
                if (single != null)
                {
                    var pkg = JSON.ToObject<Package>(single);
                    result.Add(pkg);
                }
            }

            return result;
        }

        public async Task<IEnumerable<Package>> All()
        {
            var result = await Db.QueryAsync<string>("select PackageData from Packages");
            var pkgs = result.Select(JSON.ToObject<Package>);
            return pkgs;
        }

        public void Dispose() => Db?.Dispose();

        public bool Empty()
        {
            var result = Db.Query<int>("select count(1) from Packages").Single();
            return result == 0;
        }
    }
}

namespace Winston
{
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
                    repo = JSON.ToObject<Repo>(File.ReadAllText(resolvedPath));
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
