using System;
using System.IO;
using Winston.Packaging;

namespace Winston.Cache
{
    static class LocalFileRepo
    {
        public static bool CanLoad(string uriOrPath)
        {
            Uri p;
            return Uri.TryCreate(uriOrPath, UriKind.RelativeOrAbsolute, out p);
        }

        public static bool TryLoad(string uriOrPath, out PackageSource packageSource)
        {
            try
            {
                Uri path;
                if (!Uri.TryCreate(uriOrPath, UriKind.RelativeOrAbsolute, out path))
                {
                    packageSource = null;
                    return false;
                }
                var resolvedPath = path.IsAbsoluteUri ? path.LocalPath : uriOrPath;
                if (File.Exists(resolvedPath))
                {
                    packageSource = PackageSource.FromJson(File.OpenRead(resolvedPath), uriOrPath);
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to refresh PackageSource");
                Console.Error.WriteLine(e);
            }
            packageSource = null;
            return false;
        }
    }
}