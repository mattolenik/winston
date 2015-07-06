using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Winston
{
    public class Config
    {
        public static Config Default
        {
            get
            {
                return new Config
                {
                };
            }
        }
    }

    public interface IConfigProvider : IDisposable
    {
    }

    public class ConfigProvider : IConfigProvider
    {
        public Config Config { get; private set; }

        readonly String path = Path.Combine(Paths.WinstonDir, "config.yml");

        public ConfigProvider()
        {
            if (!File.Exists(path))
            {
                Config = Config.Default;
                return;
            }

            try
            {
                LoadConfig();
            }
            catch (Exception)
            {
                Console.Error.WriteLine("Error reading config from {0}, either fix or delete this config file.", path);
                throw;
            }
        }

        void SaveConfig()
        {
            var serializer = new Serializer();
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, Config);
            }
        }

        void LoadConfig()
        {
            var deserializer = new Deserializer();
            using (var reader = new StreamReader(path))
            {
                Config = deserializer.Deserialize<Config>(reader);
            }
        }

        public void Dispose()
        {
            try
            {
                SaveConfig();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unable to save config to path {0}", path);
                Console.Error.WriteLine(e);
            }
        }
    }
}
