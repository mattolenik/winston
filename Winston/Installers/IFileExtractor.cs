using System.Threading.Tasks;

namespace Winston.Installers
{
    interface IFileExtractor
    {
        Task<string> InstallAsync(Progress progress);
    }
}