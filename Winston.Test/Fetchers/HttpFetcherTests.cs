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
        [InlineData("/realpkg", "test-0.5.zip")]
        [InlineData("/redirectedpkg", "test-0.5.zip")]
        [InlineData("/realpkgnotype", "package")]
        [InlineData("/test-0.5.zip", "test-0.5.zip")]
        public async Task CanFetch(string uri, string expectedFileName)
        {
            var fetcher = new HttpFetcher();
            var pkg = new Package
            {
                Name = "Test",
                Location = new Uri(fixture.Prefix + uri)
            };
            var tmpPkg = await fetcher.FetchAsync(pkg, null);
            tmpPkg.FileName.Should().Be(expectedFileName, "should have been inferred from content-disposition header or URL");
            using (var tmpDir = new TempDirectory("winston-test-"))
            {
                var ext = new ArchiveExtractor();
                await ext.ExtractAsync(tmpPkg, tmpDir, null);
                Directory.GetFiles(tmpDir).Should().Contain(f => f.Contains("test.exe"));
            }
        }
    }
}