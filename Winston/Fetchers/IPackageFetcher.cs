using System.Threading.Tasks;
using Winston.Packaging;

namespace Winston.Fetchers
{
    public interface IPackageFetcher
    {
        Task<TempPackage> FetchAsync(Package pkg, Progress progress);
        bool IsMatch(Package pkg);
    }
}