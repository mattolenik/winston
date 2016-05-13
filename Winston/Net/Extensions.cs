using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Winston.Net
{
    static class Extensions
    {
        public static IList<Tuple<string,string>> ParseQueryString(this Uri uri)
        {
            var result = new List<Tuple<string, string>>();
            var query = uri.Query;

            // remove anything other than query string from url
            if (query.Contains("?"))
            {
                query = query.Substring(query.IndexOf('?') + 1);
            }

            foreach (string vp in Regex.Split(query, "&"))
            {
                var singlePair = Regex.Split(vp, "=");
                if (singlePair.Length == 2)
                {
                    result.Add(Tuple.Create(Uri.UnescapeDataString(singlePair[0]), Uri.UnescapeDataString(singlePair[1])));
                }
                else
                {
                    // only one key with no value specified in query string
                    result.Add(Tuple.Create(Uri.UnescapeDataString(singlePair[0]), string.Empty));
                }
            }

            return result;
        }

        public static async Task DownloadFileAsync(this HttpClient client, Uri url, Stream output, IProgress<double> progress)
        {
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"The request returned with HTTP status code {response.StatusCode}");
                }
                var total = response.Content.Headers.ContentLength ?? -1L;

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var totalRead = 0L;
                    var buffer = new byte[81920]; // Default size from .NET docs on CopyTo
                    while (true)
                    {
                        var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            break;
                        }
                        await output.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        // Can't report progress if there was no Content-Length
                        if (total > 0)
                        {
                            progress?.Report(totalRead * 1d / (total * 1d) * 100);
                        }
                    }
                    progress?.Report(100);
                }
            }
        }
    }
}
