﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Winston.Test.Acceptance
{
    [Collection("Acceptance")]
    public class InstallAndBootstrap
    {
        readonly PortableInstallFixture fix;

        public InstallAndBootstrap(PortableInstallFixture fix)
        {
            this.fix = fix;
            var p = winst("add index http://localhost:9500/index.json");
            p.ExitCode.Should().Be(0);
        }

        SimpleProcess winst(string args = null) => fix.winst(args);

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
            var winstonExe = Path.Combine(fix.InstallDirectory, "winston.exe");
            var ver = AssemblyName.GetAssemblyName(winstonExe).Version.ToString();
            var p = winst();

            p.StdOut.Should().Contain("Winston v", "Expect Winston version string output");
            p.StdOut.Should().Contain(ver, "Bootstrapped Winston version should equal build version");
        }


        [Fact]
        public void CanInstallPackage()
        {
            var p = winst("install FakePackage");
            p.ExitCode.Should().Be(0);

            // TODO: rename "repo" path segment
            File.Exists(Path.Combine(fix.Installer.WinstonHome.Path, "repo", "FakePackage", "latest", "fake.exe"))
                .Should()
                .BeTrue("Mock package should exist");
            p = SimpleProcessTools.cmd("winston.exe list installed", fix.InstallDirectory).Run();
            p.ExitCode.Should().Be(0);
            p.StdOut.Should().Contain("FakePackage").And.Contain($"From {PortableInstallFixture.Prefix}/fake.exe");
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
            var p = winst("search fakepackage");
            p.ExitCode.Should().Be(ExitCodes.Ok);
            p.StdOut.Should().Contain("FakePackage").And.Contain("fake.exe");
        }

        [Fact]
        public void SearchForNotFoundReturnsError()
        {
            var p = winst("search nonexistant");
            p.ExitCode.Should().Be(ExitCodes.PackageNotFound);
        }

        [Fact]
        public void ListAvailableShowsPackages()
        {
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

        [Fact]
        public void ListNoArgsGetsHelpResponse()
        {
            var p = winst("list");
            p.ExitCode.Should().Be(ExitCodes.InvalidArgument);
            p.StdOut.Should().Contain("List what?");
        }

        [Fact]
        public void InfoShowsPackage()
        {
            var p = winst("info fakepackage");
            p.ExitCode.Should().Be(ExitCodes.Ok);
            p.StdOut.Should()
                .Contain("Name")
                .And.Contain("FakePackage")
                .And.Contain("Description")
                .And.Contain("fake desc");
        }

        [Fact]
        public void InfoForNotFoundReturnsError()
        {
            var p = winst("info nonexistant");
            p.ExitCode.Should().Be(ExitCodes.PackageNotFound);
            p.StdOut.Should().Contain("No package");
        }
    }
}