using DiscordScriptBot.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DiscordScriptBot.Event;

namespace DiscordScriptBot.Script
{
    public class ScriptManager
    {
        public interface IScriptMeta
        {
            string Name { get; }
            string Description { get; }
            ulong Guild { get; } // TODO
            string EventTrigger { get; }
            string Author { get; }
            DateTime CreationDate { get; }
        }

        private class ScriptDefinition : IScriptMeta
        {
            // Metadata
            public string Name { get; set; }
            public string Description { get; set; }
            public ulong Guild { get; set; }
            public string EventTrigger { get; set; }
            public string Author { get; set; }
            public DateTime CreationDate { get; set; }

            public bool Enabled { get; set; }
            public BlockExpression Tree { get; set; }
        }

        private Config _config;
        private ScriptExecutor _executor;
        private EventDispatcher _dispatcher;
        private Dictionary<string, Type> _tagMappings;
        private Dictionary<string, ScriptDefinition> _scriptDefs;

        public ScriptManager(Config config, ScriptExecutor executor, EventDispatcher dispatcher)
        {
            _config = config;
            _executor = executor;
            _dispatcher = dispatcher;
            _tagMappings = new Dictionary<string, Type>() { { "scriptDef", typeof(ScriptDefinition) },
                                                            { "ref", typeof(CallExpression.ClassRef) } };
            _scriptDefs = new Dictionary<string, ScriptDefinition>();

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

            if (Directory.Exists(config.ScriptsDir))
            {
                var deserializer = GetSerializerBuilder<DeserializerBuilder>().Build();
                foreach (string file in Directory.GetFiles(config.ScriptsDir))
                {
                    ScriptDefinition script;
                    try
                    {
                        string content = File.ReadAllText(file);
                        script = deserializer.Deserialize<ScriptDefinition>(content);
                        if (script.EventTrigger.Length == 0)
                            throw new Exception("Empty event trigger"); // call exception handler below
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to load script {Path.GetFileName(file)}: {e.Message}");
                        continue;
                    }

                    Console.WriteLine($"Loaded script: {script.Name}");
                    AddScript(script);
                }
            }
        }

        public void AddScript(string name, string description, string author, BlockExpression tree)
        {
            var definition = new ScriptDefinition
            {
                Name = name,
                Description = description,
                Author = author,
                CreationDate = DateTime.Now,
                Enabled = true,
                Tree = tree
            };
            AddScript(definition);
            SaveScript(definition.Name);
        }

        private void AddScript(ScriptDefinition script)
        {
            Debug.Assert(!_scriptDefs.ContainsKey(script.Name));
            _scriptDefs.Add(script.Name, script);

            // If this script isn't enabled, we're done..
            if (!script.Enabled)
            {
                Console.WriteLine($"Script disabled: {script.Name}");
                return;
            }

            script.Enabled = false;
            if (!SetScriptEnabled(script.Name, true, false))
                _scriptDefs.Remove(script.Name);
        }

        public bool SetScriptEnabled(string name, bool enabled, bool save = false)
        {
            if (!_scriptDefs.ContainsKey(name) || _scriptDefs[name].Enabled == enabled)
                return false;

            var script = _scriptDefs[name];
            if (enabled)
            {
                // Subscribe the script to its requested event trigger.
                // If this fails, it means the requested event is invalid/unsupported.
                if (!_dispatcher.SubscribeScript(script.EventTrigger, script.Name))
                {
                    Console.WriteLine($"Failed to SubscribeScript on dispatcher: {script.EventTrigger}");
                    return false;
                }

                // Compile the script, and if that succeeds, add the script.
                // Otherwise revert and remove the script from the dispatcher.
                if (!_executor.AddScript(script, script.Tree))
                {
                    _dispatcher.UnsubscribeScript(script.EventTrigger, script.Name);
                    Console.WriteLine($"Failed to AddScript on executor: {script.Name}");
                    return false;
                }
            }
            else
            {
                _dispatcher.UnsubscribeScript(script.EventTrigger, script.Name);
                _executor.RemoveScript(script.Name);
            }

            script.Enabled = enabled;
            if (save)
                SaveScript(name);
            return true;
        }

        public void RemoveScript(string name)
        {
            if (!_scriptDefs.ContainsKey(name))
                return;

            SetScriptEnabled(name, false, false);
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
