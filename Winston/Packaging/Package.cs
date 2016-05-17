using System;
using System.Collections.Generic;
using Winston.Serialization;

namespace Winston.Packaging
{
    public class Package
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public Uri Location { get; set; }

        public string Sha1 { get; set; }

        public string Path { get; set; }

        public PackageType Type { get; set; }

        public List<string> Preserve { get; set; } = new List<string>();

        public List<string> Ignore { get; set; } = new List<string>();

        public Platform Platform { get; set; }

        public List<Package> Variants { get; set; } = new List<Package>();

        public string Version { get; set; }

        public string ResolveVersion()
        {
            return Version ?? Sha1;
        }

        public Package Merge(Package other)
        {
            return new Package
            {
                Name = Name ?? other.Name,
                Description = Description ?? other.Description,
                Maintainer = Maintainer ?? other.Maintainer,
                Location = Location ?? other.Location,
                Path = Path ?? other.Path,
                Type = Type != PackageType.Nil ? Type : other.Type,
                Preserve = Preserve ?? other.Preserve,
                Ignore = Ignore ?? other.Ignore,
                Platform = Platform != Platform.Nil ? Platform : other.Platform,
                Variants = Variants ?? other.Variants,
                Version = Version ?? other.Version
            };
        }

        public override string ToString()
        {
            return $"{Name}@{Location}";
        }

        public string GetListing()
        {
            var result = $"- {Name} {Version}";
            if (!string.IsNullOrWhiteSpace(Description))
            {
                result += $"\n  {Description}";
            }
            result += $"\n  From {Location}\n";
            return result;
        }

        public string GetInfo()
        {
            return Json.ToJson(this, true);
        }
    }
}