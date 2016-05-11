using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using fastJSON;
using Winston.Net;
using Winston.Packaging;
using Environment = Winston.OS.Environment;

namespace Winston.Index
{
    class WebIndex
    {
        public static async Task<PackageSource> TryLoadAsync(string uriOrPath)
        {
            Uri uri;
            if (!Uri.TryCreate(uriOrPath, UriKind.Absolute, out uri))
            {
                return null;
            }
            var scheme = uri.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https")
            {
                // TODO: log here
                return null;
            }

            try
            {
                using (var client = NetUtils.HttpClient())
                {
                    var res = await client.GetAsync(uri);
                    var json = await res.Content.ReadAsStringAsync();
                    var result = JSON.ToObject<PackageSource>(json);
                    result.Location = uri.ToString();
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (Environment.IsDebug)
                {
                    Console.Error.Write(ex.StackTrace);
                }
            }
            return null;
        }
    }
}
