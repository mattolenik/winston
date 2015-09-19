using System;
using System.IO;
using Winston.Serialization;

namespace Winston
{
    public class Config
    {
        /// <summary>
        /// The directory in which Winston will install itself.
        /// </summary>
        public string WinstonDir { get; set; }
    }

    public interface IConfigProvider
    {
        Config Config { get; }
    }

    public class ConfigProvider : IConfigProvider
    {
        public Config Config { get; private set; }

        static readonly Config Default = new Config
        {
            WinstonDir = Path.Combine(Paths.AppData, @"winston\")
        };

        readonly string path = Path.Combine(Paths.ExecutingDir, "config.yml");

        public ConfigProvider()
        {
            if (!File.Exists(path))
            {
                Config = Default;
                return;
            }
            try
            {
                LoadConfig();
            }
            catch (Exception e)
            {
                throw new Exception($"Error reading configuration from '{path}'", e);
            }
        }
        void LoadConfig()
        {
            Config = Yml.Load<Config>(path);
        }
    }
}