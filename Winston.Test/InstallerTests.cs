using System;
using FluentAssertions;
using Xunit;

namespace Winston.Test
{
    public class InstallerTests
    {
        [Fact(Skip="Work in progress")]
        public void BootstrapsCorrectly()
        {
            var path = Paths.GetDirectory(typeof(Winmain).Assembly.Location);
            using (var installer = new Winstall(path))
            {
                var res = installer.Bootstrap();
                res.Should().Be(0);
            }
        }
    }
}