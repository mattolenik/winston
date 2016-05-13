using System;
using System.Net.Http;
using System.Reflection;

namespace Winston.Net
{
    public static class NetUtils
    {
        static readonly string Version = Assembly.GetExecutingAssembly().RealVersion();
        static readonly string UserAgent = $"winston/{Version}";

        public static HttpClient HttpClient(HttpClientHandler handler = null)
        {
            var c = new HttpClient(handler ?? new HttpClientHandler());
            c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            return c;
        }
    }
}