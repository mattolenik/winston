using System;
using System.IO;
using FluentAssertions;
using Winston.OS;
using Xunit;

namespace Winston.Test
{
    [Collection("JunctionPoint")]
    public class JunctionPointTests
    {
        string tempFolder;

        public JunctionPointTests()
        {
            tempFolder = Path.GetTempFileName();
            File.Delete(tempFolder);
            Directory.CreateDirectory(tempFolder);
        }

        public void Dispose()
        {
            if (tempFolder != null)
            {
                foreach (FileSystemInfo file in new DirectoryInfo(tempFolder).GetFileSystemInfos())
                {
                    file.Delete();
                }

                Directory.Delete(tempFolder);
                tempFolder = null;
            }
        }

        [Fact]
        public void Exists_NoSuchFile()
        {
            JunctionPoint.Exists(Path.Combine(tempFolder, "$$$NoSuchFolder$$$")).Should().BeFalse();
        }

        [Fact]
        public void Exists_IsADirectory()
        {
            File.Create(Path.Combine(tempFolder, "AFile")).Close();

            JunctionPoint.Exists(Path.Combine(tempFolder, "AFile")).Should().BeFalse();
        }

        [Fact]
        public void Create_VerifyExists_GetTarget_Delete()
        {
            string targetFolder = Path.Combine(tempFolder, "ADirectory");
            string junctionPoint = Path.Combine(tempFolder, "SymLink");

            Directory.CreateDirectory(targetFolder);
            File.Create(Path.Combine(targetFolder, "AFile")).Close();

            // Verify behavior before junction point created.
            File.Exists(Path.Combine(junctionPoint, "AFile")).Should().BeFalse("File should not be located until junction point created.");

            JunctionPoint.Exists(junctionPoint).Should().BeFalse("Junction point not created yet.");

            // Create junction point and confirm its properties.
            JunctionPoint.Create(junctionPoint, targetFolder, false /*don't overwrite*/);

            JunctionPoint.Exists(junctionPoint).Should().BeTrue("Junction point exists now.");

            targetFolder.ShouldBeEquivalentTo(JunctionPoint.GetTarget(junctionPoint));

            File.Exists(Path.Combine(junctionPoint, "AFile")).Should().BeTrue("File should be accessible via the junction point.");

            // Delete junction point.
            JunctionPoint.Delete(junctionPoint);

            JunctionPoint.Exists(junctionPoint).Should().BeFalse("Junction point should not exist now.");

            File.Exists(Path.Combine(junctionPoint, "AFile")).Should().BeFalse("File should not be located after junction point deleted.");

            Directory.Exists(junctionPoint).Should().BeFalse("Ensure directory was deleted too.");

            // Cleanup
            File.Delete(Path.Combine(targetFolder, "AFile"));
        }

        [Fact]
        public void Create_ThrowsIfOverwriteNotSpecifiedAndDirectoryExists()
        {
            string targetFolder = Path.Combine(tempFolder, "ADirectory");
            string junctionPoint = Path.Combine(tempFolder, "SymLink");

            Directory.CreateDirectory(junctionPoint);

            Action test = () => JunctionPoint.Create(junctionPoint, targetFolder, false);
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void Create_OverwritesIfSpecifiedAndDirectoryExists()
        {
            string targetFolder = Path.Combine(tempFolder, "ADirectory");
            string junctionPoint = Path.Combine(tempFolder, "SymLink");

            Directory.CreateDirectory(junctionPoint);
            Directory.CreateDirectory(targetFolder);

            JunctionPoint.Create(junctionPoint, targetFolder, true);

            targetFolder.Should().Be(JunctionPoint.GetTarget(junctionPoint));
        }

        [Fact]
        public void Create_ThrowsIfTargetDirectoryDoesNotExist()
        {
            string targetFolder = Path.Combine(tempFolder, "ADirectory");
            string junctionPoint = Path.Combine(tempFolder, "SymLink");

            Action test = () => JunctionPoint.Create(junctionPoint, targetFolder, false);
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void GetTarget_NonExistentJunctionPoint()
        {
            Action test = () => JunctionPoint.GetTarget(Path.Combine(tempFolder, "SymLink"));
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void GetTarget_CalledOnADirectoryThatIsNotAJunctionPoint()
        {
            Action test = () => JunctionPoint.GetTarget(tempFolder);
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void GetTarget_CalledOnAFile()
        {
            File.Create(Path.Combine(tempFolder, "AFile")).Close();

            Action test = () => JunctionPoint.GetTarget(Path.Combine(tempFolder, "AFile"));
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void Delete_NonExistentJunctionPoint()
        {
            // Should do nothing.
            JunctionPoint.Delete(Path.Combine(tempFolder, "SymLink"));
        }

        [Fact]
        public void Delete_CalledOnADirectoryThatIsNotAJunctionPoint()
        {
            Action test = () => JunctionPoint.Delete(tempFolder);
            test.ShouldThrow<IOException>();
        }

        [Fact]
        public void Delete_CalledOnAFile()
        {
            File.Create(Path.Combine(tempFolder, "AFile")).Close();

            Action test = () => JunctionPoint.Delete(Path.Combine(tempFolder, "AFile"));
            test.ShouldThrow<IOException>();
        }
    }
}
