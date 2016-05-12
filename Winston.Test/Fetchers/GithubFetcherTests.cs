using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Winston.Extractors;
using Xunit;
using Winston.Fetchers;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Test.Fetchers
{
    public class GithubFetcherTests
    {
        [Theory]
        [InlineData("test-0.5.zip")]
        [InlineData("test-*.zip&name=!*src.zip")]
        public async Task CanFetchLatestVersion(string namePattern)
        {
            var fetcher = new GithubFetcher();
            var pkg = new Package
            {
                Name = "Test",
                Location = new Uri($"github://mattolenik/winston-test/?name={namePattern}")
            };
            var tmpPkg = await fetcher.FetchAsync(pkg, null);
            var ext = new ArchiveExtractor();
            using (var tmpDir = new TempDirectory())
            {
                await ext.ExtractAsync(tmpPkg, tmpDir, null);
                Directory.GetFiles(tmpDir).Should().Contain(f => f.Contains("test.exe"));
            }
        }
    }
}