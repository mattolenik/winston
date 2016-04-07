using System.Collections.Generic;

namespace Winston.Cache
{
    /// <summary>
    /// Represents sets of names of changed packages when an index is updated.
    /// </summary>
    public class PackageChanges
    {
        public IEnumerable<string> Removed { get; set; } = new string[] {};

        public IEnumerable<string> Added { get; set; } = new string[] {};

        public IEnumerable<string> Updated { get; set; } = new string[] {};
    }
}