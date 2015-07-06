
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

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

            var name = pkg.Name.ToLowerInvariant();
            var installPath = Path.Combine(cellarPath, name, hash);
            Directory.CreateDirectory(installPath);
            using (var file = File.OpenRead(tmpFile))
            {
                Unzip(file, installPath);
            }

            var appPath = Path.Combine(installPath, pkg.Exec);
            var relAppPath = GetRelativePath(binPath, appPath);
            var relWorkingDir = GetRelativePath(binPath, installPath);
            var alias = Path.GetFileNameWithoutExtension(appPath);
            Link(relAppPath, relWorkingDir, alias);
        }

        public void Link(string relativeAppPath, string relativeWorkingDir, string alias)
        {
            var aliasPath = Path.Combine(binPath, alias + ".exe");
            File.Copy(@"D:\Dev\Projects\winston\wrap\Debug\wrap.exe", aliasPath, true);
            using (var file = File.Open(aliasPath, FileMode.Open, FileAccess.ReadWrite))
            using (var wrap = new Wrapper(file, relativeAppPath, relativeWorkingDir))
            {
            }
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