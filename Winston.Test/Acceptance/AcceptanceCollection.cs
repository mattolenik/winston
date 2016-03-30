using Xunit;

namespace Winston.Test.Acceptance
{
    [CollectionDefinition("Acceptance")]
    public class AcceptanceCollection : ICollectionFixture<PortableInstallFixture>
    {
    }
}