namespace Winston
{
    public interface IConfigProvider
    {
        string GetWinstonDir();
        bool WriteRegistryPath { get; }
    }
}