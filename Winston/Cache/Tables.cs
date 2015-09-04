namespace Winston.Cache
{
    class Tables
    {
        public class Packages
        {
            static readonly string name = nameof(Packages);

            public static readonly string CreateStatement = $@"
    CREATE TABLE IF NOT EXISTS `{name}` (
	`Name`	TEXT NOT NULL,
	`Description`	TEXT,
	`PackageData`	TEXT NOT NULL,
    PRIMARY KEY(Name)
)";

            public static readonly string DeleteAllStatement = $@"delete from `{name}`";
        }

        public class Sources
        {
            static readonly string name = nameof(Sources);

            public static readonly string CreateStatement =$@"
    CREATE TABLE IF NOT EXISTS `{name}` (
	`URI`	TEXT NOT NULL,
	PRIMARY KEY(URI)
)";

            public static readonly string DeleteAllStatement = $@"delete from `{name}`";
        }

        public class PackageSearch
        {
            static readonly string name = nameof(PackageSearch);

            public static readonly string CreateStatement = $@"CREATE VIRTUAL TABLE IF NOT EXISTS `{name}` USING fts3(Name, Desc)";

            public static readonly string DeleteStatement = $@"delete from `{name}`";
        }
    }
}
