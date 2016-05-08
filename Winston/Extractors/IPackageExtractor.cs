using System;
using System.Threading.Tasks;
using Winston.Fetchers;

namespace Winston.Extractors
{
    public interface IPackageExtractor
    {
        Task ExtractAsync(TempPackage package, string destination, Progress progress);
        bool IsMatch(TempPackage package);
    }
}