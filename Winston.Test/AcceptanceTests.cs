using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using HttpMock;
using Xunit;
using static Winston.Test.SimpleProcessTools;

namespace Winston.Test
{
    [Collection("Acceptance")]
    public class AcceptanceTests : IDisposable
    {
        [Fact]
        public void BootstrapsCorrectly()
        {
            var envPathBefore = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            var envPathAfter = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            envPathBefore.Should().NotBeSameAs(envPathAfter);
            envPathAfter.Should().NotBeNullOrWhiteSpace();
            envPathAfter?.Split(';')
                .First()
                .Should()
                .Match(p => Directory.Exists(p), "Injected path must exist")
                .And
                .Match(p => Directory.GetFiles(p).Any(f => f.Contains("winston.exe")), "Must contain winston.exe");
        }

        [Fact]
        public void SelfPackageInstallsCorrectly()
        {
            var winstonExe = Path.Combine(installDir, "winston.exe");
            var ver = AssemblyName.GetAssemblyName(winstonExe).Version.ToString();
            var p = winst();

            p.StdOut.Should().Contain("Winston v", "Expect Winston version string output");
            p.StdOut.Should().Contain(ver, "Bootstrapped Winston version should equal build version");
        }

        [Fact]
        public void CanAddIndex()
        {
            var p = winst("add index http://localhost:9500/index.json");
            p.ExitCode.Should().Be(0);
        }

        [Fact]
        public void CanInstallPackage()
        {
            CanAddIndex();

            var p = winst("install FakePackage");
            p.ExitCode.Should().Be(0);

            // TODO: rename "repo" path segment
            File.Exists(Path.Combine(installer.WinstonHome.Path, "repo", "FakePackage", "latest", "fake.exe"))
                .Should()
                .BeTrue("Mock package should exist");
            p = cmd("winston.exe list installed", installDir).Run();
            p.ExitCode.Should().Be(0);
            p.StdOut.Should().Contain("FakePackage").And.Contain($"From {Prefix}/fake.exe");
        }

        [Fact]
        public void InstallReturnsErrorForNotFoundPackage()
        {
            var p = winst("install nonexistant");
            p.ExitCode.Should().Be(ExitCodes.PackageNotFound);
            p.StdOut.Should().Contain("No packages found matching nonexistant");
        }

        [Fact]
        public void SearchReturnsPackage()
        {
            CanAddIndex();
            var p = winst("search fakepackage");
            p.ExitCode.Should().Be(ExitCodes.Ok);
            p.StdOut.Should().Contain("FakePackage").And.Contain("fake.exe");
        }

        [Fact]
        public void SearchForNotFoundReturnsError()
        {
            CanAddIndex();
            var p = winst("search nonexistant");
            p.ExitCode.Should().Be(ExitCodes.PackageNotFound);
        }

        [Fact]
        public void ListAvailableShowsPackages()
        {
            CanAddIndex();
            var p = winst("list available");
            p.ExitCode.Should().Be(ExitCodes.Ok);
            p.StdOut.Should().Contain("FakePackage").And.Contain("fake.exe");
            p.StdOut.Should().Contain("NothingPackage").And.Contain("nothing.zip");
        }

        [Fact]
        public void ListInstalledShowsPackages()
        {
            CanInstallPackage();
            var p = winst("list installed");
            p.ExitCode.Should().Be(ExitCodes.Ok);
            p.StdOut.Should().Contain("FakePackage").And.Contain("fake.exe");
            p.StdOut.Should().NotContain("NothingPackage").And.NotContain("nothing.zip");
        }

        SimpleProcess winst(string arguments = "") => cmd($"winston.exe {arguments}", installDir).Run();

        readonly IHttpServer server;

        readonly Winstall installer;

        readonly string installDir;

        const string Prefix = "http://localhost:9500";

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

        public AcceptanceTests()
        {
            Environment.SetEnvironmentVariable("WINSTON_DEBUG", "1");
            server = HttpMockRepository.At(Prefix);
            StubRepo();
            server.Start();

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetAbsolutePath());
            installer = new Winstall(path);
            var res = installer.Bootstrap(TimeSpan.FromSeconds(60));
            res.Should().Be(0);
            installDir = Path.Combine(installer.WinstonHome.Path, "repo", "winston", "latest");
            Directory.Exists(installDir).Should().BeTrue("Installed directory should exist");
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
            installer.Dispose();
        }
    }
}