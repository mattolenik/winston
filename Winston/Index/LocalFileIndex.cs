using System;
using System.IO;
using Winston.Packaging;
using Environment = Winston.OS.Environment;

namespace Winston.Index
{
    static class LocalFileIndex
    {
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
                // TODO: proper logging
                if (Environment.IsDebug)
                {
                    Console.Error.WriteLine($"Unable to load local file index '{uriOrPath}'");
                    Console.Error.WriteLine(e);
                }
            }
            packageSource = null;
            return false;
        }
    }
}