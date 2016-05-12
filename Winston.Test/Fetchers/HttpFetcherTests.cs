using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Winston.Extractors;
using Winston.Fetchers;
using Winston.OS;
using Winston.Packaging;
using Xunit;

namespace Winston.Test.Fetchers
{
    public class HttpFetcherTests : IClassFixture<HttpPackageFixture>
    {
        readonly HttpPackageFixture fixture;

        public HttpFetcherTests(HttpPackageFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("/realpkg")]
        [InlineData("/redirectedpkg")]
        public async Task CanFetch(string uri)
        {
            var fetcher = new HttpFetcher();
            var pkg = new Package
            {
                Name = "Test",
                Location = new Uri(fixture.Prefix + uri)
            };
            var tmpPkg = await fetcher.FetchAsync(pkg, null);
            var ext = new ArchiveExtractor();
            using (var tmpDir = new TempDirectory("winston-test-"))
            {
                await ext.ExtractAsync(tmpPkg, tmpDir, null);
                Directory.GetFiles(tmpDir).Should().Contain(f => f.Contains("test.exe"));
            }
        }
    }
}