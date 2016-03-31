using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fastJSON;
using Winston.Index;
using Winston.Packaging;
using Environment = Winston.OS.Environment;

namespace Winston.Cache
{
    public sealed class SqliteCache : IDisposable
    {
        readonly string dbPath;
        internal SQLiteConnection Db { get; private set; }

        SqliteCache(string dbPath)
        {
            this.dbPath = dbPath;
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
        }

        public static async Task<SqliteCache> CreateAsync(string winstonDir)
        {
            var dbPath = Path.Combine(winstonDir, "cache.sqlite");
            var cache = new SqliteCache(dbPath);
            await cache.LoadAsync();
            return cache;
        }

        async Task<bool> TryCreateTablesAsync() => await Task.Run(() =>
        {
            using (var t = Db.BeginTransaction())
            {
                try
                {
                    Db.Execute(Tables.Packages.CreateStatement, transaction: t);
                    Db.Execute(Tables.Sources.CreateStatement, transaction: t);
                    Db.Execute(Indexes.PackageIndex.CreateStatement, transaction: t);
                    Db.Execute(Tables.PackageSearch.CreateStatement, transaction: t);
                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.Rollback();
                    Console.Error.WriteLine(ex);
                    return false;
                }
            }
            return true;
        });

        async Task LoadAsync()
        {
            Db = new SQLiteConnection($"Data Source={dbPath}");
            await Db.OpenAsync();
            var canContinue = await TryCreateTablesAsync();
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
                canContinue = await TryCreateTablesAsync();
                if (!canContinue)
                {
                    throw new Exception("Unable to create cache database");
                }
            }
        }

        public async Task AddIndexAsync(string uriOrPath)
        {
            uriOrPath = new Uri(uriOrPath).AbsoluteUri.ToLowerInvariant().TrimEnd('/', '\\');

            PackageSource r;
            if (LocalFileIndex.TryLoad(uriOrPath, out r))
            {
                await AddIndexToDbAsync(r);
                return;
            }
            r = await WebIndex.TryLoadAsync(uriOrPath);
            if (r != null)
            {
                await AddIndexToDbAsync(r);
                return;
            }
            throw new Exception($"Unable to load index at '{uriOrPath}'");
        }

        public async Task<IEnumerable<string>> GetIndexesAsync()
        {
            var indexes = await Db.QueryAsync<string>("select Location From Sources");
            return indexes;
        }

        async Task AddIndexToDbAsync(PackageSource r)
        {
            using (var t = Db.BeginTransaction())
            {
                try
                {
                    foreach (var pkg in r.Packages)
                    {
                        var json = JSON.ToJSON(pkg);
                        var result = Db.Execute("insert or replace into Sources (Location) values (@Location)", new { Location = r.Location });
                        result = await Db.ExecuteAsync(
                            @"insert or replace into Packages (Name, Description, PackageData) values (@Name, @Desc, @Data)",
                            new {Name = pkg.Name, Desc = pkg.Description, Data = json},
                            transaction: t);
                        result = await Db.ExecuteAsync(
                            @"insert or replace into PackageSearch (Name, Desc) values (@Name, @Desc)",
                            new {Name = pkg.Name, Desc = pkg.Description},
                            transaction: t);
                    }
                    t.Commit();
                }
                catch (Exception ex)
                {
                    // TODO: Log here
                    if (Environment.IsDebug)
                    {
                        Console.Error.WriteLine(ex.ToString());
                    }
                    t.Rollback();
                    throw;
                }
            }
        }

        public async Task RefreshAsync()
        {
            var repos = await Db.QueryAsync<string>("select Location from Sources");
            foreach (var repo in repos)
            {
                await AddIndexAsync(repo);
            }
        }

        public async Task<Package> ByNameAsync(string pkgName)
        {
            var result = await Db.QueryAsync<string>("select PackageData from Packages where Name = @Name", new { Name = pkgName });
            var json = result.SingleOrDefault();
            if (json == null) return null;
            var pkg = JSON.ToObject<Package>(json);
            return pkg;
        }

        public async Task<IList<Package>> ByNamesAsync(IEnumerable<string> names)
        {
            // SQLite is going to end up executing these all in serial anyway,
            // little point in making this code parallel.
            var result = new List<Package>();
            foreach (var name in names)
            {
                var match = await ByNameAsync(name);
                if (match != null)
                {
                    result.Add(match);
                }
            }
            return result;
        }

        public async Task<IList<Package>> SearchAsync(string query)
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

        public async Task<IEnumerable<Package>> AllAsync()
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