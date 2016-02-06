using System;
using System.IO;
using System.Threading.Tasks;

namespace Winston.Installers
{
    public interface IPackageInstaller : IDisposable
    {
        Task<DirectoryInfo> InstallAsync(Progress progress );
        Task<Exception> ValidateAsync();
    }
}
