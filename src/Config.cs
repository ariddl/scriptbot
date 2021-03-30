using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordScriptBot
{
    public class Config
    {
        private static readonly INamingConvention NamingConvention = CamelCaseNamingConvention.Instance;

        public string Token { get; set; } = null;
        public string ScriptsDir { get; set; } = "scripts/";
        public int ScriptPoolSize { get; set; } = 100;
        public int Tasks { get; set; } = 4;
        public bool LogDiscord { get; set; } = true;
        public bool LogExecution { get; set; } = true;

        public static Config Load(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("Configuration file is missing. Generating default.");
                return new Config().Save(file);
            }

            string content = File.ReadAllText(file);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NamingConvention)
                .Build();
            return deserializer.Deserialize<Config>(content);
        }

        public Config Save(string file)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(NamingConvention)
                .Build();
            File.WriteAllText(file, serializer.Serialize(this));
            return this;
        }

        public bool Validate() => Token != null;
    }
}
