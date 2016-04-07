using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        public static async Task<SqliteCache> CreateAsync(string winstonDir, string defaultIndex = null)
        {
            var dbPath = Path.Combine(winstonDir, "cache.sqlite");
            var cache = new SqliteCache(dbPath);
            await cache.LoadAsync();
            if (defaultIndex != null)
            {
                await cache.AddIndexAsync(defaultIndex);
            }
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

        /// <summary>
        /// Adds a local or remote index at the specified URI or path.
        /// </summary>
        /// <param name="uriOrPath">the index location</param>
        /// <param name="forceRefresh">if the index is already present in the cache,
        /// force it to be refreshed; otherwise, re-adding an index is a no-op</param>
        /// <returns>an async task for this operation</returns>
        public async Task<PackageChanges> AddIndexAsync(string uriOrPath, bool forceRefresh = false)
        {
            uriOrPath = new Uri(uriOrPath).AbsoluteUri.ToLowerInvariant().TrimEnd('/', '\\');

            // If the index exists, don't re-add it unless requested
            if (!forceRefresh)
            {
                var idexes = await GetIndexesAsync();
                if (idexes.Contains(uriOrPath, StringComparer.OrdinalIgnoreCase))
                {
                    return new PackageChanges();
                }
            }

            PackageSource r;
            if (LocalFileIndex.TryLoad(uriOrPath, out r))
            {
                return await AddIndexToDbAsync(r);
            }
            r = await WebIndex.TryLoadAsync(uriOrPath);
            if (r != null)
            {
                return await AddIndexToDbAsync(r);
            }
            throw new Exception($"Unable to load index at '{uriOrPath}'");
        }

        public async Task<IEnumerable<string>> GetIndexesAsync()
        {
            var indexes = await Db.QueryAsync<string>("select Location from Sources");
            return indexes;
        }

        async Task<PackageChanges> AddIndexToDbAsync(PackageSource r)
        {
            using (var t = Db.BeginTransaction())
            {
                try
                {
                    var pkgs = await Db.QueryAsync<string>(
                        "select Name from Packages where SourceIndex = @SourceIndex",
                        new { SourceIndex = r.Location });
                    var pkgsList = pkgs.ToList();
                    var newPkgs = r.Packages.Select(p => p.Name).ToList();
                    var removed = pkgsList.Except(newPkgs).ToList();
                    var added = newPkgs.Except(pkgsList);

                    foreach (var pkg in r.Packages)
                    {
                        var json = JSON.ToJSON(pkg);
                        var result = await Db.ExecuteAsync("insert or replace into Sources (Location) values (@Location)", new { Location = r.Location });
                        result = await Db.ExecuteAsync(
                            @"insert or replace into Packages (Name, SourceIndex, Description, PackageData) values (@Name, @SourceIndex, @Desc, @Data)",
                            new
                            {
                                Name = pkg.Name,
                                SourceIndex = r.Location,
                                Desc = pkg.Description,
                                Data = json
                            },
                            transaction: t);
                        result = await Db.ExecuteAsync(
                            @"insert or replace into PackageSearch (Name, Desc) values (@Name, @Desc)",
                            new
                            {
                                Name = pkg.Name,
                                Desc = pkg.Description
                            },
                            transaction: t);
                    }

                    foreach (var pkg in removed)
                    {
                        var result = await Db.ExecuteAsync(
                            "delete from Packages where Name = @Name and SourceIndex = @SourceIndex",
                            new
                            {
                                Name = pkg,
                                SourceIndex = r.Location
                            },
                            transaction: t);
                    }

                    t.Commit();

                    return new PackageChanges { Added = added, Removed = removed };
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
                await AddIndexAsync(repo, forceRefresh: true);
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