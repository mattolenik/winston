using System;
using System.Collections.Generic;
using System.IO;
using fastJSON;

namespace Winston.Packaging
{
    public class PackageSource : IEquatable<PackageSource>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public string Location { get; internal set; }

        public List<Package> Packages { get; set; }

        public PackageSource()
        {
        }

        public PackageSource(string location)
        {
            Location = location;
        }

        public override string ToString()
        {
            return $"Name: {Name}, Description: {Description}, Maintainer: {Maintainer}, Location: {Location}";
        }

        public bool Equals(PackageSource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Location, other.Location, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PackageSource) obj);
        }

        public override int GetHashCode()
        {
            return (Location != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Location) : 0);
        }

        public static bool operator ==(PackageSource left, PackageSource right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PackageSource left, PackageSource right)
        {
            return !Equals(left, right);
        }

        public static PackageSource FromJson(Stream openRead, string uriOrPath)
        {
            using (var sr = new StreamReader(openRead))
            {
                var pkgSrc = JSON.ToObject<PackageSource>(sr.ReadToEnd());
                pkgSrc.Location = uriOrPath;
                return pkgSrc;
            }
        }
    }
}