using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Winston.Test
{
    public class InstallerTests
    {
        [Fact]
        public void BootstrapsCorrectly()
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var path = Paths.GetDirectory(Uri.UnescapeDataString(uri.AbsolutePath));
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
    }
}