using System;
using System.IO;
using System.Threading.Tasks;

namespace Winston.Installers
{
    public interface IPackageInstaller : IDisposable
    {
        Task<DirectoryInfo> Install();
        Task<Exception> Validate();
    }
}
