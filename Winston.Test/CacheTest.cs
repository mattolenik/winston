using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Winston.Cache;
using Winston.OS;
using Xunit;
using FluentAssertions;

namespace Winston.Test
{
    public class CacheFixture : IDisposable
    {
        public SqliteCache Cache { get; private set; }
        public TempDirectory Dir { get; private set; }
        public TempFile RepoFile { get; private set; }
        public Repo Repo { get; private set; }

        public CacheFixture()
        {
            Task.Run(async () =>
            {
                Dir = new TempDirectory("winston-test-");
                Cache = await SqliteCache.Create(Dir);
                Repo = new Repo("test")
                {
                    Packages = new List<Package>
                    {
                        new Package {Name = "Pkg 1"},
                        new Package {Name = "Pkg 2", Description = "packages with even numbers, two"},
                        new Package {Name = "Pkg 3", Description = "number three is odd"},
                        new Package {Name = "Pkg 4", Description = "packages with even numbers, four"},
                    }
                };
                RepoFile = new TempFile();
                var json = Repo.ToJson();
                File.WriteAllText(RepoFile, json);
                await Cache.AddRepo(RepoFile);
            });
        }

        public void Dispose()
        {
            Cache?.Dispose();
            Dir?.Dispose();
        }
    }

    public class CacheTest : IClassFixture<CacheFixture>
    {
        readonly CacheFixture fixture;

        public CacheTest(CacheFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void FindByName()
        {
            var pkg = await fixture.Cache.ByName("Pkg 1");
            pkg?.Name.Should().Be("Pkg 1");
        }

        [Fact]
        public async void FindByNames()
        {
            var pkgs = await fixture.Cache.ByNames(new[] { "Pkg 1", "Pkg 2" });
            pkgs.Where(p => p.Name == "Pkg 1").Should().NotBeEmpty();
            pkgs.Where(p => p.Name == "Pkg 2").Should().NotBeEmpty();
        }

        [Fact]
        public async void FindByTitleSearch()
        { var pkgs = await fixture.Cache.Search("Pkg");
            pkgs.Count().Should().Be(fixture.Repo.Packages.Count);
        }

        [Fact]
        public async void FindByDescriptionSearch()
        {
            var pkgs = await fixture.Cache.Search("even");
            pkgs.Count().Should().Be(2);
            pkgs.Should().Contain(p => p.Name == "Pkg 2");
            pkgs.Should().Contain(p => p.Name == "Pkg 4");
        }

        [Fact]
        public async void GetAll()
        {
            var pkgs = await fixture.Cache.All();
            pkgs.Count().Should().Be(fixture.Repo.Packages.Count);
        }

        [Fact]
        public async void Refreshed()
        {
            var result = fixture.Cache.DB.Query("delete from Packages");
            var pkgs = await fixture.Cache.All();
            pkgs.Should().BeEmpty();
            await fixture.Cache.Refresh();
            pkgs = await fixture.Cache.All();
            pkgs.Count().Should().Be(fixture.Repo.Packages.Count);
        }
    }
}