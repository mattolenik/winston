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
