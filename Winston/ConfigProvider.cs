using System;
using System.IO;
using Winston.Serialization;

namespace Winston
{
    public class ConfigProvider : IConfigProvider
    {
        private Config Config;

        public string ResolvedWinstonDir
        {
            get
            {
                var configDir = Path.GetDirectoryName(configPath);
                if (configDir == null)
                {
                    throw new InvalidOperationException($"{nameof(configDir)} should not be null");
                }
                var result = Path.GetFullPath(Path.Combine(configDir, Config.WinstonDir));
                if (!Directory.Exists(result))
                {
                    throw new DirectoryNotFoundException($"{nameof(Config.WinstonDir)} did not resolve to a valid directory");
                }
                return result;
            }
        }

        public bool WriteRegistryPath => Config.WriteRegistryPath;

        static readonly Config Default = new Config
        {
            WinstonDir = Path.Combine(Paths.AppData, @"winston\"),
            WriteRegistryPath = true
        };

        readonly string configPath = Path.Combine(new Uri(Paths.ExecutingDir).LocalPath, "config.yml");

        public ConfigProvider()
        {
            if (!File.Exists(configPath))
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
                throw new Exception($"Error reading configuration from '{configPath}'", e);
            }
        }
        void LoadConfig()
        {
            Config = Yml.Load<Config>(configPath);
        }
    }
}