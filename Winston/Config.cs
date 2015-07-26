using System;
using System.IO;
using Winston.Serialization;
using YamlDotNet.Serialization;

namespace Winston
{
    public class Config
    {
        /// <summary>
        /// The directory in which Winston will install itself. Winston will create
        /// a subdirectory, %WinstonRoot%\winston, and install and run itself from there.
        /// </summary>
        public string WinstonRoot { get; set; }

        [YamlIgnore]
        public string WinstonDir => Path.Combine(WinstonRoot, @"winston\");
    }

    public interface IConfigProvider
    {
        Config Config { get; }
    }

    public class ConfigProvider : IConfigProvider
    {
        public Config Config { get; private set; }

        static readonly Config Default = new Config { WinstonRoot = Paths.AppData };

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