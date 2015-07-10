
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
        readonly string cellarPath;
        readonly string binPath;
        string tmp;

        public Cellar(string winstonDir)
        {
            cellarPath = Path.Combine(winstonDir, @"cellar\");
            binPath = Path.Combine(winstonDir, @"bin\");
            tmp = Path.GetTempPath();
            Directory.CreateDirectory(cellarPath);
            Directory.CreateDirectory(binPath);
        }

        public async Task Add(Package pkg)
        {
            using (var client = new PackageClient(pkg, Path.Combine(cellarPath, pkg.Name)))
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
                throw new InvalidDataException("Package '{0}' does not seem to be installed".Fmt(pkg));
            }
            var appDir = Path.GetDirectoryName(installPath);
            var relAppPath = GetRelativePath(binPath, installPath);
            var relWorkingDir = GetRelativePath(binPath, appDir);
            var alias = Path.GetFileNameWithoutExtension(installPath);
            var aliasPath = Path.Combine(binPath, alias + ".exe");

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
            var appPath = Path.Combine(cellarPath, name);
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
                var aliasPath = Path.Combine(binPath, alias + ".exe");
                File.Delete(aliasPath);
            });
        }

        public async Task<Package[]> List()
        {
            var pkgFiles = Directory.GetFiles(cellarPath, "pkg.yml", SearchOption.AllDirectories);
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