namespace Winston.OS
{
    /// <summary>
    /// Sort of a no-op temporary path item, for special cases when temporary
    /// behavior is unwanted.
    /// </summary>
    class NonTempItem : ITempItem
    {
        public void Dispose() { }

        public string Path { get; }

        public NonTempItem(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
