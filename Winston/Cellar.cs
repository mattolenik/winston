
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace Winston
{
    public class Cellar
    {
        string path;
        string tmp;

        public Cellar(string cellarPath)
        {
            path = cellarPath;
            tmp = Path.GetTempPath();
            Directory.CreateDirectory(path);
        }

        public async Task Add(Package pkg)
        {
            var c = new HttpClient();
            var res = await c.GetAsync(pkg.FetchUrl);
            using (var stream = await res.Content.ReadAsStreamAsync())
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var appPath = Path.Combine(path, pkg.Name);
                Directory.CreateDirectory(appPath);
                zip.ExtractToDirectory(appPath);
            }
        }
    }
}