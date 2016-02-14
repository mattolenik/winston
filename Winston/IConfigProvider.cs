namespace Winston
{
    public interface IConfigProvider
    {
        string ResolvedWinstonDir { get; }
        bool WriteRegistryPath { get; }
    }
}