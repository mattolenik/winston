
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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
            var c = new HttpClient();
            var res = await c.GetAsync(pkg.FetchUrl);
            var tmpFile = Path.GetTempFileName();
            using (var body = await res.Content.ReadAsStreamAsync())
            using (var file = File.OpenWrite(tmpFile))
            {
                await body.CopyToAsync(file);
            }
            string hash;
            using (var file = File.OpenRead(tmpFile))
            {
                hash = GetSha1(file);
            }

            if (!string.Equals(pkg.Sha1, hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Hash of remote file {0} did not match expected {1}".Fmt(hash, pkg.Sha1));
            }

            // Save package information to disk first. Other actions can use this
            // to interact with a package without having to load whole repos into memory.
            var pkgPath = Path.Combine(cellarPath, pkg.Name, "pkg.yml");
            Yml.Save(pkg, pkgPath);

            var installPath = Path.Combine(cellarPath, pkg.Name, hash);
            await Task.Run(() =>
            {
                Directory.CreateDirectory(installPath);
                using (var file = File.OpenRead(tmpFile))
                {
                    Unzip(file, installPath);
                }
            });

            File.Delete(tmpFile);

            await Link(pkg);
        }

        public async Task Link(Package pkg)
        {
            var installPath = Path.Combine(cellarPath, pkg.Name, pkg.Sha1);
            if (!Directory.Exists(installPath))
            {
                throw new InvalidOperationException("Cannot link app {0} because it is not installed".Fmt(pkg.Name));
            }
            var appPath = Path.Combine(installPath, pkg.Exec);
            var relAppPath = GetRelativePath(binPath, appPath);
            var relWorkingDir = GetRelativePath(binPath, installPath);
            var alias = Path.GetFileNameWithoutExtension(appPath);
            var aliasPath = Path.Combine(binPath, alias + ".exe");

            using (var wrap = new MemoryStream(Resources.wrap, 0, Resources.wrap.Length, true, true))
            using (var wrapper = new Wrapper(wrap, relAppPath, relWorkingDir, pkg.Shell))
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
                var alias = Path.GetFileNameWithoutExtension(pkg.Exec);
                var aliasPath = Path.Combine(binPath, alias + ".exe");
                File.Delete(aliasPath);
            });
        }

        public IEnumerable<Package> List()
        {
            var pkgFiles = Directory.GetFiles(cellarPath, "pkg.yml", SearchOption.AllDirectories);
            var tasks = pkgFiles.Select(p => Task.Run(() =>
            {
                var deserializer = new Deserializer();
                using (var reader = new StreamReader(p))
                {
                    return deserializer.Deserialize<Package>(reader);
                }
            })).ToArray();
            Task.WaitAll(tasks);
            return tasks.Select(t => t.Result);
        }

        void Unzip(Stream stream, string destination)
        {
            Directory.Delete(destination, true);
            using (var zip = new ZipArchive(stream))
            {
                zip.ExtractToDirectory(destination);
            }
        }

        string GetSha1(Stream stream)
        {
            var sha = new SHA1CryptoServiceProvider();
            byte[] hash;
            hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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