using System;
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
            using (var installer = new Winstall(path))
            {
                var res = installer.Bootstrap(TimeSpan.FromSeconds(60));
                res.Should().Be(0);
            }
        }
    }
}