using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using fastJSON;
using Winston.Cache;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Test.Cache
{
    public class CacheFixture : IDisposable
    {
        public SqliteCache Cache { get; private set; }
        public TempDirectory Dir { get; private set; }
        public TempFile RepoFile { get; private set; }
        public PackageSource PackageSource { get; private set; }

        public CacheFixture()
        {
            Task.Run(async () =>
            {
                Dir = TempDirectory.New("winston-test-");
                Cache = await SqliteCache.CreateAsync(Dir);
                PackageSource = new PackageSource("test")
                {
                    Packages = new List<Package>
                    {
                        new Package {Name = "Pkg 1", Location = new Uri("http://winston.ms/test")},
                        new Package {Name = "Pkg 2", Description = "packages with even numbers, two"},
                        new Package {Name = "Pkg 3", Description = "number three is odd"},
                        new Package {Name = "Pkg 4", Description = "packages with even numbers, four"},
                    }
                };
                RepoFile = new TempFile();
                var json = JSON.ToJSON(PackageSource);
                File.WriteAllText(RepoFile, json);
                await Cache.AddIndexAsync(RepoFile);
            }).Wait();
        }

        public void Dispose()
        {
            Cache?.Dispose();
            Dir?.Dispose();
        }
    }
}