using System;
using System.IO;
using System.Net;
using System.Reflection;
using HttpMock;

namespace Winston.Test.Fetchers
{
    public class HttpPackageFixture : IDisposable
    {
        readonly IHttpServer server;
        public readonly string Prefix = "http://localhost:9501";

        public HttpPackageFixture()
        {
            server = HttpMockRepository.At(new Uri(Prefix));
            var path = Path.Combine(Assembly.GetExecutingAssembly().Directory(), "testdata", "test-0.5.zip");
            server.Stub(x => x.Get("/realpkg")).ReturnFile(path).OK();
            server.Stub(x => x.Head("/realpkg")).Return("").OK();
            server.Stub(x => x.Head("/redirectedpkg"))
                .Return("")
                .AddHeader("Location", $"{Prefix}/realpkg")
                .WithStatus(HttpStatusCode.Moved);
            server.Start();
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}
