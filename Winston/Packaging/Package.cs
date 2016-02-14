using System;
using System.Collections.Generic;

namespace Winston.Packaging
{
    public class Package
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public Uri URL { get; set; }

        public string Filename { get; set; }

        public string SHA1 { get; set; }

        public string Path { get; set; }

        public PackageType Type { get; set; }

        public PackageFileType FileType { get; set; }

        public List<string> Preserve { get; set; } = new List<string>();

        public List<string> Ignore { get; set; } = new List<string>();

        public Platform Platform { get; set; }

        public List<Package> Variants { get; set; } = new List<Package>();

        public string Version { get; set; }

        public string ResolveVersion()
        {
            // TODO: check if null check is sufficient or if IsNullOrWhitespace is needed. Depends on serialization behavior.
            return Version ?? SHA1;
        }

        public Package Merge(Package other)
        {
            return new Package
            {
                Name = Name ?? other.Name,
                Description = Description ?? other.Description,
                Maintainer = Maintainer ?? other.Maintainer,
                URL = URL ?? other.URL,
                Filename = Filename ?? other.Filename,
                Path = Path ?? other.Path,
                Type = Type != PackageType.Nil ? Type : other.Type,
                FileType = FileType != PackageFileType.Nil ? FileType : other.FileType,
                Preserve = Preserve ?? other.Preserve,
                Ignore = Ignore ?? other.Ignore,
                Platform = Platform != Platform.Nil ? Platform : other.Platform,
                Variants = Variants ?? other.Variants,
                Version = Version ?? other.Version
            };
        }

        public override string ToString()
        {
            var result = $"- Name: {Name}\n  URI: {URL}\n";
            if (Version != null) result += $"Version: {Version}";
            return result;
        }
    }
}