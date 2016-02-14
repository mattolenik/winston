using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using fastJSON;
using Winston.Cache;
using Winston.OS;
using Xunit;
using FluentAssertions;
using Winston.Packaging;

namespace Winston.Test
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
                Dir = new TempDirectory("winston-test-");
                Cache = await SqliteCache.CreateAsync(Dir);
                PackageSource = new PackageSource("test")
                {
                    Packages = new List<Package>
                    {
                        new Package {Name = "Pkg 1", URL = new Uri("http://winston.ms/test")},
                        new Package {Name = "Pkg 2", Description = "packages with even numbers, two"},
                        new Package {Name = "Pkg 3", Description = "number three is odd"},
                        new Package {Name = "Pkg 4", Description = "packages with even numbers, four"},
                    }
                };
                RepoFile = new TempFile();
                var json = JSON.ToJSON(PackageSource);
                File.WriteAllText(RepoFile, json);
                await Cache.AddRepoAsync(RepoFile);
            }).Wait();
        }

        public void Dispose()
        {
            Cache?.Dispose();
            Dir?.Dispose();
        }
    }

    public class CacheTest : IClassFixture<JsonConfig>, IClassFixture<CacheFixture>
    {
        readonly CacheFixture fixture;

        public CacheTest(JsonConfig cfg, CacheFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void FindByName()
        {
            var pkg = await fixture.Cache.ByNameAsync("Pkg 1");
            pkg?.Name.Should().Be("Pkg 1");
        }

        [Fact]
        public async void FindByNames()
        {
            var pkgs = await fixture.Cache.ByNamesAsync(new[] { "Pkg 1", "Pkg 2" });
            pkgs.Where(p => p.Name == "Pkg 1").Should().NotBeEmpty();
            pkgs.Where(p => p.Name == "Pkg 2").Should().NotBeEmpty();
        }

        [Fact]
        public async void FindByTitleSearch()
        {
            var pkgs = await fixture.Cache.SearchAsync("Pkg");
            pkgs.Count().Should().Be(fixture.PackageSource.Packages.Count);
        }

        [Fact]
        public async void FindByDescriptionSearch()
        {
            var pkgs = await fixture.Cache.SearchAsync("even");
            pkgs.Count().Should().Be(2);
            pkgs.Should().Contain(p => p.Name == "Pkg 2");
            pkgs.Should().Contain(p => p.Name == "Pkg 4");
        }

        [Fact]
        public async void GetAll()
        {
            var pkgs = await fixture.Cache.AllAsync();
            pkgs.Count().Should().Be(fixture.PackageSource.Packages.Count);
        }

        [Fact]
        public async void Refreshed()
        {
            var result = fixture.Cache.Db.Query("delete from Packages");
            var pkgs = await fixture.Cache.AllAsync();
            pkgs.Should().BeEmpty();
            await fixture.Cache.RefreshAsync();
            pkgs = await fixture.Cache.AllAsync();
            pkgs.Count().Should().Be(fixture.PackageSource.Packages.Count);
        }
    }
}