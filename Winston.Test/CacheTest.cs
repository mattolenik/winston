using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NSpec;
using Winston.Cache;
using Winston.OS;

namespace Winston.Test
{
    class CacheTest : nspecAsync
    {
        SqliteCache cache;
        TempDirectory dir;
        TempFile repoFile;
        Repo repo;

        void before_each() => Task.Run(async () =>
        {
            dir = new TempDirectory("winston-test-");
            cache = await SqliteCache.Create(dir);
            repo = new Repo("test")
            {
                Packages = new List<Package>
                {
                    new Package { Name = "Pkg 1" },
                    new Package { Name = "Pkg 2", Description = "packages with even numbers, two" },
                    new Package { Name = "Pkg 3", Description = "number three is odd" },
                    new Package { Name = "Pkg 4", Description = "packages with even numbers, four" },
                }
            };
            repoFile = new TempFile();
            var json = repo.ToJson();
            File.WriteAllText(repoFile, json);
            await cache.AddRepo(repoFile);
        }).Wait();

        void after_each()
        {
            cache?.Dispose();
            dir?.Dispose();
        }

        void describe_cache()
        {
            it["can find by name"] = async () =>
            {
                var pkg = await cache.ByName("Pkg 1");
                pkg?.Name.should_be("Pkg 1");
            };

            it["can find by names"] = async () =>
            {
                var pkgs = await cache.ByNames(new[] { "Pkg 1", "Pkg 2" });
                pkgs.Where(p => p.Name == "Pkg 1").should_not_be_empty();
                pkgs.Where(p => p.Name == "Pkg 2").should_not_be_empty();
            };

            it["can find by title search"] = async () =>
            {
                var pkgs = await cache.Search("Pkg");
                pkgs.Count().should_be(repo.Packages.Count);
            };

            it["can find by desc search"] = async () =>
            {
                var pkgs = await cache.Search("even");
                pkgs.Count().should_be(2);
                pkgs.should_contain(p => p.Name == "Pkg 2");
                pkgs.should_contain(p => p.Name == "Pkg 4");
            };

            it["can get all"] = async () =>
            {
                var pkgs = await cache.All();
                pkgs.Count().should_be(repo.Packages.Count);
            };

            it["can be refreshed"] = async () =>
            {
                var result = cache.DB.Query("delete from Packages");
                var pkgs = await cache.All();
                pkgs.should_be_empty();
                await cache.Refresh();
                pkgs = await cache.All();
                pkgs.Count().should_be(repo.Packages.Count);
            };
        }
    }
}