using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using HttpMock;
using Xunit;
using static Winston.Test.SimpleProcessTools;

namespace Winston.Test
{
    public class BootstrapTests : IDisposable
    {
        [Fact]
        public void BootstrapsCorrectly()
        {
            var envPathBefore = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
            using (var installer = new Winstall(path))
            {
                var res = installer.Bootstrap(TimeSpan.FromSeconds(60));
                res.Should().Be(0);
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
        }

        [Fact]
        public void PackagesInstallCorrectly()
        {
            using (var installer = new Winstall(path))
            {
                var res = installer.Bootstrap(TimeSpan.FromSeconds(60));
                res.Should().Be(0);

                var installedDir = Path.Combine(installer.WinstonHome.Path, "repo", "winston", "latest");
                var winstonExe = Path.Combine(installedDir, "winston.exe");
                var ver = AssemblyName.GetAssemblyName(winstonExe).Version.ToString();
                var p = cmd("winston.exe", installedDir).Run(TimeSpan.FromSeconds(60));

                p.StdOut.Should().Contain("Winston v", "Expect Winston version string output");
                p.StdOut.Should().Contain(ver, "Bootstrapped Winston version should equal build version");
            }
        }

        [Fact]
        public async Task PackageInstallsFromWeb()
        {
            // TODO: Real test
            var c = new HttpClient();
            var res = await c.GetAsync("http://localhost:9500/test.txt");
            var str = await res.Content.ReadAsStringAsync();
            Console.WriteLine(str);
            str.Should().Be("sdf");
        }

        readonly string path;

        readonly IHttpServer server;

        public BootstrapTests()
        {
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetAbsolutePath());
            server = HttpMockRepository.At("http://localhost:9500");
            server.Stub(x => x.Get("/test.txt")).ReturnFile(Path.Combine(Paths.ExecutingDirPath, "testdata", "test.txt")).OK();
            server.Start();
        }

        public void Dispose()
        {
            server?.Dispose();
        }
    }
}