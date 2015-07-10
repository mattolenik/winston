using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace Winston.Installers
{
    static class Content
    {
        public static string MatchContentDispositionFileExt(HttpContentHeaders headers, params string[] extensions)
        {
            if (!headers.Contains("Content-Disposition")) return null;

            var disposition = headers.GetValues("Content-Disposition").SingleOrDefault();
            if (string.IsNullOrWhiteSpace(disposition)) return null;

            var cd = new ContentDisposition(disposition);

            if (string.IsNullOrWhiteSpace(cd.FileName)) return null;

            return extensions.Any(ext => cd.FileName.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)) ? cd.FileName : null;
        }

        public static bool ContentTypeMatches(HttpContentHeaders headers, params string[] contentTypes)
        {
            var ct = headers.GetValues("Content-Type").SingleOrDefault() ?? "";
            return contentTypes.Any(contentType => string.Equals(ct, contentType, StringComparison.OrdinalIgnoreCase));
        }

        public static string MatchUriFileExt(Uri uri, params string[] extensions)
        {
            var filename = uri.Segments.Last();
            return extensions.Any(ext => filename.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)) ? filename : null;
        }
    }
}