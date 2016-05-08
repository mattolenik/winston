namespace Winston.OS
{
    class ExistingPathItem : IDisposablePathItem
    {
        public void Dispose() { }

        public string Path { get; }

        public ExistingPathItem(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
