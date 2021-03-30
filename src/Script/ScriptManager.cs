using DiscordScriptBot.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DiscordScriptBot.Script
{
    public class ScriptManager
    {
        public interface IScriptMeta
        {
            string Name { get; }
            string Description { get; }
            string Guild { get; set; } // TODO
            string EventTrigger { get; }
            string Author { get; }
            DateTime CreationDate { get; }
            public bool Enabled { get; set; }
            public BlockExpression Tree { get; set; }
        }

        public class ScriptDefinition : IScriptMeta
        {
            // Metadata
            public string Name { get; set; }
            public string Description { get; set; }
            public string Guild { get; set; }
            public string EventTrigger { get; set; }
            public string Author { get; set; }
            public DateTime CreationDate { get; set; }

            public bool Enabled { get; set; }
            public BlockExpression Tree { get; set; }
        }

        private Config _config;
        private Dictionary<string, Type> _tagMappings;
        private Dictionary<string, ScriptDefinition> _scriptDefs;

        public ScriptManager(Config config)
        {
            _config = config;

            _tagMappings = new Dictionary<string, Type>() { { "scriptDef", typeof(ScriptDefinition) },
                                                            { "ref", typeof(CallExpression.ClassRef) } };
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.GetInterfaces().Contains(typeof(IExpression)))
                        continue;

                    // Get the tag key from this type in camel case
                    IExpression expr = (IExpression)Activator.CreateInstance(type);
                    string tagKey = expr.Type.ToString();
                    tagKey = char.ToLowerInvariant(tagKey[0]) + tagKey.Substring(1);

                    Debug.Assert(!_tagMappings.ContainsKey(tagKey));
                    _tagMappings.Add(tagKey, type);
                }
            }

            _scriptDefs = new Dictionary<string, ScriptDefinition>();
            if (Directory.Exists(config.ScriptsDir))
            {
                var deserializer = GetSerializerBuilder<DeserializerBuilder>().Build();
                foreach (string file in Directory.GetFiles(config.ScriptsDir))
                {
                    string content = File.ReadAllText(file);

                    try
                    {
                        var script = deserializer.Deserialize<ScriptDefinition>(content);
                        _scriptDefs.Add(script.Name, script);
                        Console.WriteLine($"Loaded script {script.Name}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load script {Path.GetFileName(file)}: {e.Message}");
                    }
                }
            }
        }

        public void AddScript(string name, string description, string author, IExpression tree)
        {
            var definition = new ScriptDefinition
            {
                Name = name,
                Description = description,
                Author = author,
                CreationDate = DateTime.Now,
                Enabled = true
            };
            Debug.Assert(!_scriptDefs.ContainsKey(definition.Name));
            _scriptDefs.Add(definition.Name, definition);
            SaveScript(definition.Name);
        }

        public void RemoveScript(string name)
        {
            if (!_scriptDefs.ContainsKey(name))
                return;

            new FileInfo(GetScriptFile(_scriptDefs[name])).Delete();
            _scriptDefs.Remove(name);
        }

        public void SaveScript(string name)
        {
            if (!_scriptDefs.ContainsKey(name))
                return;
            
            var definition = _scriptDefs[name];
            var serializer = GetSerializerBuilder<SerializerBuilder>().Build();

            if (!Directory.Exists(_config.ScriptsDir))
                Directory.CreateDirectory(_config.ScriptsDir);
            File.WriteAllText(GetScriptFile(definition), serializer.Serialize(definition));
        }

        public IScriptMeta[] GetScripts(bool enabledOnly)
            => _scriptDefs.Values.Where(d => enabledOnly ? d.Enabled : true).ToArray();

        public IScriptMeta GetScript(string name)
            => _scriptDefs.ContainsKey(name) ? _scriptDefs[name] : null;

        private string GetScriptFile(ScriptDefinition d) => $"{_config.ScriptsDir}/{d.Name}.yaml";

        private T GetSerializerBuilder<T>() where T : BuilderSkeleton<T>, new()
        {
            T builder = new T()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);

            // The following omits the Type property from IExpression
            // and ensures deserialization will succeed upon loading.
            if (builder is SerializerBuilder serializer)
                serializer.EnsureRoundtrip();

            foreach (var tag in _tagMappings)
                builder.WithTagMapping($"tag:yaml.org,2002:{tag.Key}", tag.Value);
            return builder;
        }
    }
}
