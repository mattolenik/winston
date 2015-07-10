using System;
using System.Threading.Tasks;

namespace Winston.Installers
{
    public interface IPackageInstaller
    {
        Task<string> Install();
        Task<Exception> Validate();
    }
}
