using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winston.Cache
{
    class Indexes
    {
        public class PackageIndex
        {
            public static readonly string CreateStatement = @"CREATE INDEX IF NOT EXISTS `PackageIndex` ON `Packages` (`Name` ASC)";
        }
    }
}
