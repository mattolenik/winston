using System.Linq;
using System.Threading.Tasks;

namespace Winston
{
    static class CellarExtensions
    {
        public static void AddApps(this Cellar cellar, Cache cache, params string[] appNames)
        {
            Task.WaitAll(appNames.Select(cache.ByName).Select(async pkg => await cellar.Add(pkg)).ToArray());
        }

        public static async Task RemoveApps(this Cellar cellar, params string[] apps)
        {
            await Task.WhenAll(apps.Select(async appName => await cellar.Remove(appName)));
        }
    }
}
