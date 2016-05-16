using System;
using System.IO;
using Winston.Serialization;

namespace Winston
{
    public class ConfigProvider : IConfigProvider
    {
        private Config Config;

        public string GetWinstonDir()
        {
            // This resolves even if configPath doesn't exist
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

        public bool WriteRegistryPath => Config.WriteRegistryPath;

        public string DefaultIndex => Config.DefaultIndex;

        static readonly Config Default = new Config
        {
            WinstonDir = Path.Combine(Paths.AppData, "winston"),
            WriteRegistryPath = true,
            DefaultIndex = "https://raw.githubusercontent.com/mattolenik/winston-packages/master/sample.json"
        };

        readonly string configPath = Path.Combine(Paths.ExecutingDirPath, "winston.cfg");

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
            Config = Json.Load<Config>(configPath);
        }
    }
}