using System;

namespace Winston.Test
{
    public sealed class JsonConfig : IDisposable
    {
        public JsonConfig()
        {
            Serialization.JsonConfig.Init();
        }

        public void Dispose()
        {
        }
    }
}