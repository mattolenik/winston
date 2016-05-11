using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using HttpMock;
using Winston.OS;
using Environment = System.Environment;

namespace Winston.Test
{
    public class PortableInstallFixture : IDisposable
    {
        public SimpleProcess winst(string arguments = "") => SimpleProcess.Cmd($"winston.exe {arguments}", InstallDirectory).Run();

        readonly IHttpServer server;

        public readonly Winstall Installer;

        public readonly string InstallDirectory;

        public string Prefix => prefix;

        static readonly string prefix = "http://localhost:9500";

        public readonly string IndexJSON = new
        {
            Name = "Test Index",
            Packages = new[]
            {
                new
                {
                    Location = $"{prefix}/fake.exe",
                    Name = "FakePackage",
                    Type = "Binary"
                },
                new
                {
                    Location = $"{prefix}/nothing.zip",
                    Name = "NothingPackage",
                    Type = "Archive"
                }
            }
        }.ToJSON();

        public string IndexBody { get; set; }

        public PortableInstallFixture()
        {
            RestoreIndex();
            Environment.SetEnvironmentVariable("WINSTON_DEBUG", "1");
            server = HttpMockRepository.At(Prefix);
            StubRepo();
            server.Start();

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetAbsolutePath());
            Installer = new Winstall(path);
            var res = Installer.Bootstrap(TimeSpan.FromSeconds(60));
            res.Should().Be(0);
            InstallDirectory = Path.Combine(Installer.WinstonHome.Path, "repo", "winston", "latest");
            Directory.Exists(InstallDirectory).Should().BeTrue("Installed directory should exist");
        }

        void StubRepo()
        {
            server.Stub(x => x.Get("/index.json")).Return(() => IndexBody).OK();
            server.Stub(x => x.Get("/fake.exe")).Return("").OK();
            server.Stub(x => x.Get("/nothing.zip")).Return("").OK();
        }

        public IRequestStub Stub(Func<RequestHandlerFactory, IRequestStub> func)
        {
            return server.Stub(func);
        }

        public void Dispose()
        {
            server.Dispose();
            Installer.Dispose();
        }

        public void RestoreIndex()
        {
            IndexBody = IndexJSON;
        }
    }
}
