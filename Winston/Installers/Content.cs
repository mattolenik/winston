using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace Winston.Installers
{
    static class Content
    {
        public static string MatchContentDispositionFileExt(string ext, HttpContentHeaders headers)
        {
            if (!headers.Contains("Content-Disposition")) return null;

            var disposition = headers.GetValues("Content-Disposition").SingleOrDefault();
            if (string.IsNullOrWhiteSpace(disposition)) return null;

            var cd = new ContentDisposition(disposition);
            var fn = cd.FileName;

            if (string.IsNullOrWhiteSpace(fn)) return null;
            return fn.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase) ? fn : null;
        }

        public static bool ContentTypeIs(string contentType, HttpContentHeaders headers)
        {
            var ct = headers.GetValues("Content-Type").SingleOrDefault() ?? "";
            return string.Equals(ct, contentType, StringComparison.OrdinalIgnoreCase);
        }

        public static string MatchUriFileExt(Uri uri, string ext)
        {
            var filename = uri.Segments.Last();
            return filename.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase) ? filename : null;
        }
    }
}