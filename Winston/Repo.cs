
using System;

namespace Winston
{
    public class Repo : IEquatable<Repo>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Maintainer { get; set; }

        public string Url { get; set; }

        public Package[] Packages { get; set; }

        public Repo(string url)
        {
            Url = url;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Description: {1}, Maintainer: {2}", Name, Description, Maintainer);
        }

        public bool Equals(Repo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
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
            return Url.GetHashCode();
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

    public class Package
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string FetchUrl { get; set; }

        public string Exec { get; set; }

        public bool Shell { get; set; }

        public string Sha1 { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Description: {1}, Exec: {2}, Shell: {3}, FetchUrl: {4}", Name, Description, Exec, Shell, FetchUrl);
        }
    }
}
