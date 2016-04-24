using System.Threading.Tasks;

namespace Winston.Installers
{
    interface IPackageExtractor
    {
        Task<string> InstallAsync(Progress progress);
    }
}