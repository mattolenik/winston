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

        /// <summary>
        /// Whether or not to write to the registry and update the user's PATH.
        /// Usually false for bootstrapped/embedded installations.
        /// </summary>
        public bool WriteRegistryPath { get; set; }
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
            WinstonDir = Path.Combine(Paths.AppData, @"winston\"),
            WriteRegistryPath = true
        };

        readonly string path = Path.Combine(new Uri(Paths.ExecutingDir).LocalPath, "config.yml");

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