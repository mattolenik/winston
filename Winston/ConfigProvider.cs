using System;
using System.IO;
using Winston.Serialization;

namespace Winston
{
    public class ConfigProvider : IConfigProvider
    {
        private Config Config;

        public string ResolvedWinstonDir
            => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), Config.WinstonDir));

        public bool WriteRegistryPath => Config.WriteRegistryPath;

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