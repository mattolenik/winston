using System;
using System.IO;
using System.Threading.Tasks;
using Winston.User;

namespace Winston.Installers
{
    public interface IPackageInstaller : IDisposable
    {
        Task<DirectoryInfo> Install(Progress progress );
        Task<Exception> Validate();
    }
}
