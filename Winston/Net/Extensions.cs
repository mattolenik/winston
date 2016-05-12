﻿using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Winston.Net
{
    static class Extensions
    {
        public static NameValueCollection ParseQueryString(this Uri uri)
        {
            var result = new NameValueCollection();
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
                    result.Add(Uri.UnescapeDataString(singlePair[0]), Uri.UnescapeDataString(singlePair[1]));
                }
                else
                {
                    // only one key with no value specified in query string
                    result.Add(Uri.UnescapeDataString(singlePair[0]), string.Empty);
                }
            }

            return result;
        }
    }
}