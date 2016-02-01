using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using fastJSON;
using NativeInjector;
using Winston.OS;
using Winston.User;
using YamlDotNet.Serialization;
using Environment = Winston.OS.Environment;
using static NativeInjector.Utils;

namespace Winston
{
    public class Cellar
    {
        readonly UserProxy user;

        public string CellarPath { get; }

        public string BinPath { get; }

        public Cellar(UserProxy user, string winstonDir)
        {
            this.user = user;
            CellarPath = Path.Combine(winstonDir, @"cellar\");
            BinPath = Path.Combine(winstonDir, @"bin\");
            Path.GetTempPath();
            Directory.CreateDirectory(CellarPath);
            Directory.CreateDirectory(BinPath);
        }

        public async Task Add(Package pkg)
        {
            var pkgDir = Path.Combine(CellarPath, pkg.Name);
            var client = new PackageClient(pkg, pkgDir);
            var progress = user.NewProgress(pkg.Name);
            var installDir = await client.Install(progress);
            var junctionPath = CreateCurrentJunction(pkgDir, installDir.FullName);
            var pathVal = Path.Combine(junctionPath, pkg.Path ?? "");
            PathLink(pathVal);
            progress.CompletedInstall();
        }

        static string CreateCurrentJunction(string pkgDir, string installDir)
        {
            var junction = Path.Combine(pkgDir, "latest");
            JunctionPoint.Create(junction, installDir, true);
            return junction;
        }

        static string RemoveCurrentJunction(string pkgDir)
        {
            var junction = Path.Combine(pkgDir, "latest");
            if (!JunctionPoint.Exists(junction)) return null;
            JunctionPoint.Delete(junction);
            return junction;
        }

        static void PathLink(string installPath)
        {
            var dir = Paths.GetDirectory(installPath);
            Environment.AddToPath(dir, @"winston\cellar");
            var pid = ParentProcessId((uint)Process.GetCurrentProcess().Id);
            if (pid == null)
            {
                throw new Exception("Unable to get parent process ID");
            }
            var here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dll32 = Path.Combine(here, EnvUpdate.Dll32Name);
            string dll64 = Path.Combine(here, EnvUpdate.Dll64Name);
            Injector.Inject(pid.Value, dll32, dll64, EnvUpdate.SharedMemName(pid.Value), EnvUpdate.Prepend(installPath));
        }

        static void PathUnlink(string installPath)
        {
            Environment.RemoveFromPath(installPath);
        }

        public async Task Remove(string name)
        {
            var pkgDir = Path.Combine(CellarPath, name);
            if (!Directory.Exists(pkgDir))
            {
                return;
            }
            try
            {
                // Make best attempt to unlink. If it fails, it won't prevent linking during a future reinstall.
                var junction = RemoveCurrentJunction(pkgDir);
                if (junction != null) PathUnlink(junction);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            await Task.Run(() => Directory.Delete(pkgDir, true));
        }

        public async Task<Package[]> List()
        {
            var pkgFiles = Directory.GetFiles(CellarPath, "pkg.json", SearchOption.AllDirectories);
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

        public async Task Restore() => await Task.Run(() =>
        {
            var pkgs =
                Directory.GetFiles(CellarPath, "pkg.json", SearchOption.AllDirectories)
                    .Select(x => new
                    {
                        File = new FileInfo(x),
                        Pkg = JSON.ToObject<Package>(File.ReadAllText(x))
                    });
            var list = pkgs.Select(pkg => Path.Combine(pkg.File.DirectoryName, pkg.Pkg.Path ?? ""));
            // TODO: env variable injection
        });
    }
}