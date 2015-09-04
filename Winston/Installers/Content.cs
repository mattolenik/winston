using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace Winston.Installers
{
    static class Content
    {
        public static string MatchContentDispositionFileExt(NameValueCollection headers, params string[] extensions)
        {
            if (!headers.AllKeys.Contains("Content-Disposition")) return null;

            var disposition = headers["Content-Disposition"];
            if (string.IsNullOrWhiteSpace(disposition)) return null;

            var cd = new ContentDisposition(disposition);

            if (string.IsNullOrWhiteSpace(cd.FileName)) return null;

            return extensions.Any(ext => cd.FileName.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)) ? cd.FileName : null;
        }

        public static bool ContentTypeMatches(NameValueCollection headers, params string[] contentTypes)
        {
            var ct = headers["Content-Type"] ?? "";
            return contentTypes.Any(contentType => string.Equals(ct, contentType, StringComparison.OrdinalIgnoreCase));
        }

        public static string MatchUriFileExt(Uri uri, params string[] extensions)
        {
            var filename = uri.Segments.Last();
            return extensions.Any(ext => filename.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)) ? filename : null;
        }
    }
}