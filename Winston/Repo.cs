using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Winston
{
    public class Repo : IEquatable<Repo>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public string URL { get; set; }

        public List<Package> Packages { get; set; }

        public Repo(string url)
        {
            URL = url;
        }

        public override string ToString()
        {
            return $"Name: {Name}, Description: {Description}, Maintainer: {Maintainer}, URL: {URL}";
        }

        public bool Equals(Repo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(URL, other.URL, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Repo)obj);
        }

        public override int GetHashCode()
        {
            return URL.GetHashCode();
        }

        public static bool operator ==(Repo left, Repo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Repo left, Repo right)
        {
            return !Equals(left, right);
        }
    }

    public enum PackageType
    {
        UI, Shell
    }

    public enum PackageFileType
    {
        Archive, Binary
    }

    public enum Platform
    {
        Any = 0,
        x64 = 1,
        x86 = 2
    }

    public class Package
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public Uri URL { get; set; }

        public string Filename { get; set; }

        public string SHA1 { get; set; }

        public string Path { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public PackageType? Type { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public PackageFileType? FileType { get; set; }

        public List<string> Preserve { get; set; } = new List<string>();

        public List<string> Ignore { get; set; } = new List<string>();

        [JsonConverter(typeof (StringEnumConverter))]
        public Platform? Platform { get; set; }

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
                Type = Type ?? other.Type,
                FileType = FileType ?? other.FileType,
                Preserve = Preserve ?? other.Preserve,
                Ignore = Ignore ?? other.Ignore,
                Platform = Platform ?? other.Platform,
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