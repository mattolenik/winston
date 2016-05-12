using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using HttpMock;
using Winston.Extractors;
using Xunit;
using Winston.Fetchers;
using Winston.OS;
using Winston.Packaging;

namespace Winston.Test.Fetchers
{
    public class HttpFetcherTests : IDisposable
    {
        readonly IHttpServer server;
        readonly string prefix = "http://localhost:9510";

        public HttpFetcherTests()
        {
            server = HttpMockRepository.At(prefix);
            server.Start();
            var path = Path.Combine(Assembly.GetExecutingAssembly().Directory(), "testdata", "test-0.5.zip");
            server.Stub(x => x.Get("/realpkg")).ReturnFile(path);
            server.Stub(x => x.Head("/redirectedpkg"))
                .Return("")
                .AddHeader("Location", "/realpkg")
                .WithStatus(HttpStatusCode.Moved);
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
                Location = new Uri(prefix + uri)
            };
            var tmpPkg = await fetcher.FetchAsync(pkg, null);
            var ext = new ArchiveExtractor();
            using (var tmpDir = new TempDirectory())
            {
                await ext.ExtractAsync(tmpPkg, tmpDir, null);
                Directory.GetFiles(tmpDir).Should().Contain(f => f.Contains("test.exe"));
            }
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}