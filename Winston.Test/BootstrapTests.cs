using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

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
                var p = TestProcess.Shell("winston.exe");
                p.Run(TimeSpan.FromSeconds(60));
                p.StdOut.Should().Contain("Winston v", "Expect Winston version string output");
                var ver = Assembly.LoadFile(Path.Combine(path, "winston.exe")).GetName().Version.ToString();
                p.StdOut.Should().Contain(ver, "Bootstrapped Winston version should equal build version");
            }
        }

        readonly string path;

        public BootstrapTests()
        {
            path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetAbsolutePath());
        }

        public void Dispose()
        {
        }
    }
}