using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using HttpMock;
using static Winston.Test.SimpleProcessTools;

namespace Winston.Test
{
    public class PortableInstallFixture : IDisposable
    {
        public SimpleProcess winst(string arguments = "") => cmd($"winston.exe {arguments}", InstallDirectory).Run();

        readonly IHttpServer server;

        public readonly Winstall Installer;

        public readonly string InstallDirectory;

        public const string Prefix = "http://localhost:9500";

        readonly string indexJSON = new
        {
            Name = "Test Index",
            Packages = new[]
            {
                new
                {
                    Location = $"{Prefix}/fake.exe",
                    Name = "FakePackage",
                    FileType = "Binary"
                },
                new
                {
                    Location = $"{Prefix}/nothing.zip",
                    Name = "NothingPackage",
                    FileType = "Archive"
                }
            }
        }.ToJSON();

        public PortableInstallFixture()
        {
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
            server.Stub(x => x.Get("/index.json")).Return(indexJSON).OK();
            server.Stub(x => x.Get("/fake.exe")).Return("").OK();
            server.Stub(x => x.Get("/nothing.zip")).Return("").OK();
        }

        public void Dispose()
        {
            server.Dispose();
            Installer.Dispose();
        }
    }
}
