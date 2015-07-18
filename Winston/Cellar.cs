
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winston.Properties;
using YamlDotNet.Serialization;

namespace Winston
{
    public class Cellar
    {
        public string CellarPath { get; }

        public string BinPath { get; }

        public Cellar(string winstonDir)
        {
            CellarPath = Path.Combine(winstonDir, @"cellar\");
            BinPath = Path.Combine(winstonDir, @"bin\");
            Path.GetTempPath();
            Directory.CreateDirectory(CellarPath);
            Directory.CreateDirectory(BinPath);
        }

        public async Task Add(Package pkg)
        {
            using (var client = new PackageClient(pkg, CellarPath))
            {
                var installPath = await client.Install();
                await Link(pkg, installPath);
            }
        }

        public async Task Link(Package pkg, string installPath)
        {
            // If the package installer extracted an EXE, the installPath will be the full path to that EXE.
            // If the installer extracted a ZIP file, the installPath will be the directory, and the full
            // path will have to be constructed using pkg.Filename
            if (!File.Exists(installPath))
            {
                installPath = Path.Combine(installPath, pkg.Filename);
            }
            if (!File.Exists(installPath))
            {
                throw new InvalidDataException($"Package '{pkg}' does not seem to be installed");
            }
            var appDir = Path.GetDirectoryName(installPath);
            var relAppPath = GetRelativePath(BinPath, installPath);
            var relWorkingDir = GetRelativePath(BinPath, appDir);
            var alias = Path.GetFileNameWithoutExtension(installPath);
            var aliasPath = Path.Combine(BinPath, $"{alias}.exe");

            if (File.Exists(aliasPath))
            {
                var dt = DateTime.Now.ToString("yyyyMMddHHmmss");
                var oldAlias = Path.Combine(BinPath, $"{alias}.old_{dt}");
                // Works even if the process is running, gives us upgrade for free
                File.Move(aliasPath, oldAlias);
            }

            using (var wrap = new MemoryStream(Resources.wrap, 0, Resources.wrap.Length, true, true))
            using (var wrapper = new Wrapper(wrap, relAppPath, relWorkingDir, pkg.Type == PackageType.Shell))
            using (var file = File.Create(aliasPath))
            {
                wrapper.Wrap();
                wrap.Position = 0;
                var buf = wrap.GetBuffer();
                await file.WriteAsync(buf, 0, buf.Length);
            }
        }

        public async Task Remove(string name)
        {
            var appPath = Path.Combine(CellarPath, name);
            if (!Directory.Exists(appPath))
            {
                return;
            }
            var pkg = Yml.Load<Package>(Path.Combine(appPath, "pkg.yml"));
            await Unlink(pkg);

            await Task.Run(() => Directory.Delete(appPath, true));
        }

        public async Task Unlink(Package pkg)
        {
            await Task.Run(() =>
            {
                var alias = Path.GetFileNameWithoutExtension(pkg.Run);
                var aliasPath = Path.Combine(BinPath, alias + ".exe");
                File.Delete(aliasPath);
            });
        }

        public async Task<Package[]> List()
        {
            var pkgFiles = Directory.GetFiles(CellarPath, "pkg.yml", SearchOption.AllDirectories);
            var tasks = pkgFiles.Select(async p => await Task.Run(() =>
            {
                var deserializer = new Deserializer();
                using (var reader = new StreamReader(p))
                {
                    return deserializer.Deserialize<Package>(reader);
                }
            }));
            var res = await Task.WhenAll(tasks);
            return res;
        }

        static string GetRelativePath(string from, string to)
        {
            var path1 = new Uri(from);
            var path2 = new Uri(to);
            var diff = path1.MakeRelativeUri(path2);
            return diff.OriginalString;
        }
    }

}