using System.Linq;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Winston.Test.Cache
{
    public class Tests : IClassFixture<JsonConfig>, IClassFixture<CacheFixture>
    {
        readonly CacheFixture fixture;

        public Tests(JsonConfig cfg, CacheFixture fixture)
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