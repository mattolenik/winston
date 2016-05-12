using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using fastJSON;
using NativeInjector;
using Winston.OS;
using Winston.Packaging;
using Winston.User;
using Environment = Winston.OS.Environment;
using static NativeInjector.Utils;

namespace Winston
{
    // TODO: Find a better name, this sounds like a remote repo or package source
    public class Repo
    {
        readonly UserProxy user;

        public string RepoPath { get; }

        public Repo(UserProxy user, string root)
        {
            this.user = user;
            root = Path.GetFullPath(root);
            RepoPath = Path.Combine(root, @"repo\");
            Path.GetTempPath();
            Directory.CreateDirectory(RepoPath);
        }

        public async Task<string> AddAsync(Package pkg, bool inject = true, bool writeRegistryPath = true)
        {
            var pkgDir = Path.Combine(RepoPath, pkg.Name);
            var client = new PackageClient(pkg, pkgDir);
            var progress = user.NewProgress(pkg.Name);
            var installDir = await client.InstallAsync(progress);
            var junctionPath = CreateCurrentJunction(pkgDir, installDir.FullName);
            var path = Path.Combine(junctionPath, pkg.Path ?? "");
            if (writeRegistryPath)
            {
                UpdateRegistryPath(path);
            }
            if (inject)
            {
                InjectPathIntoParent(path);
            }
            progress.CompletedInstall();
            return path;
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

        static void UpdateRegistryPath(string installPath)
        {
            Environment.AddToPath(installPath, @"winston\repo");
        }

        static void InjectPathIntoParent(string installPath)
        {
            var pid = ParentProcessId((uint)Process.GetCurrentProcess().Id);
            if (pid == null)
            {
                throw new Exception("Unable to get parent process ID");
            }
            var here = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var dll32 = Path.Combine(here, EnvUpdate.Dll32Name);
            var dll64 = Path.Combine(here, EnvUpdate.Dll64Name);
            Injector.Inject(pid.Value, dll32, dll64, EnvUpdate.SharedMemName(pid.Value), EnvUpdate.Prepend(installPath));
        }

        static void PathUnlink(string installPath)
        {
            Environment.RemoveFromPath(installPath);
        }

        public async Task RemoveAsync(string name)
        {
            var pkgDir = Path.Combine(RepoPath, name);
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

        public async Task<Package[]> ListAsync()
        {
            var pkgFiles = Directory.GetFiles(RepoPath, "pkg.json", SearchOption.AllDirectories);
            var tasks = pkgFiles.Select(async p => await Task.Run(() =>
            {
                using (var reader = new StreamReader(p))
                {
                    var json = reader.ReadToEnd();
                    return JSON.ToObject<Package>(json);
                }
            }));
            var res = await Task.WhenAll(tasks);
            return res;
        }

        public async Task RestoreAsync() => await Task.Run(() =>
        {
            var pkgs =
                Directory.GetFiles(RepoPath, "pkg.json", SearchOption.AllDirectories)
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