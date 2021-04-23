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
        // Readonly interface representing script metadata.
        public interface IScriptMeta
        {
            string Name { get; }
            string Description { get; }
            ulong Guild { get; }
            string EventTrigger { get; }
            string Author { get; }
            DateTime CreationDate { get; }
            bool Enabled { get; }
        }

        // Internal mutable class representing a script type.
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

            // Load tag mappings for roundtrip YAML serialization
            LoadTagMappings();

            // Check if we have a scripts directory to load serialized scripts
            if (Directory.Exists(config.ScriptsDir))
                LoadScripts();
        }

        private void LoadTagMappings()
        {
            // Find the types representing serializable expressions (our custom Expression types).
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // Type must implement IExpression.
                    if (!type.GetInterfaces().Contains(typeof(IExpression)) || type.IsInterface)
                        continue;

                    // Get the tag key from this type in camel case. Tags are required for YAML.
                    IExpression expr = (IExpression)Activator.CreateInstance(type);
                    string tagKey = expr.Type.ToString();
                    tagKey = char.ToLowerInvariant(tagKey[0]) + tagKey.Substring(1);

                    // Add the tag mapping for the YAML serializer for roundtrip serialization.
                    Debug.Assert(!_tagMappings.ContainsKey(tagKey));
                    _tagMappings.Add(tagKey, type);
                }
            }
        }

        private void LoadScripts()
        {
            // Prepare a deserializer and iterate over the script files
            var deserializer = GetSerializerBuilder<DeserializerBuilder>().Build();
            foreach (string file in Directory.GetFiles(_config.ScriptsDir))
            {
                // Attempt to load and deserialize the script file.
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

                // Add the deserialized script.
                Console.WriteLine($"Loaded script: {script.Name}");
                AddScript(script);

                // If this script isn't enabled, we're done..
                if (!script.Enabled)
                {
                    Console.WriteLine($"Script disabled: {script.Name}");
                    continue;
                }

                if (!ActivateScript(script.Name))
                    _scriptDefs.Remove(script.Name);
            }
        }

        public void AddScript(string name, string description,
                              ulong guild, string author,
                              string @event, BlockExpression tree)
        {
            // We're adding a script that was just created; create its initial definition.
            var definition = new ScriptDefinition
            {
                Name = name,
                Description = description,
                Guild = guild,
                EventTrigger = @event,
                Author = author,
                CreationDate = DateTime.Now,
                Enabled = true,
                Tree = tree,
            };

            // Add and save the script.
            AddScript(definition);
            SaveScript(definition.Name);
        }

        private void AddScript(ScriptDefinition script)
        {
            // Ignore scripts with the same name.
            if (_scriptDefs.ContainsKey(script.Name))
            {
                Console.WriteLine($"Ignoring script with duplicate name: {script.Name}");
                return;
            }
            _scriptDefs.Add(script.Name, script);
        }

        public bool ActivateScript(string name)
        {
            if (!_scriptDefs.ContainsKey(name))
                return false;
            var script = _scriptDefs[name];

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
            return true;
        }

        public bool SetScriptEnabled(string name, bool enabled, bool save = false)
        {
            // Make sure the script exists and its state doesn't match the one we want.
            if (!_scriptDefs.ContainsKey(name) || _scriptDefs[name].Enabled == enabled)
                return false;

            var script = _scriptDefs[name];
            if (enabled)
                ActivateScript(name);
            else
            {
                _dispatcher.UnsubscribeScript(script.EventTrigger, script.Name);
                _executor.RemoveScript(script.Name);
            }

            // Update the script's enabled flag and re-save it.
            script.Enabled = enabled;
            if (save)
                SaveScript(name);
            return true;
        }

        public void RemoveScript(string name)
        {
            // Check if the script exists.
            if (!_scriptDefs.ContainsKey(name))
                return;

            // Disable the script, delete the file, and remove the definition.
            SetScriptEnabled(name, false, false);
            new FileInfo(GetScriptFile(_scriptDefs[name])).Delete();
            _scriptDefs.Remove(name);
        }

        public void SaveScript(string name)
        {
            // Check if the script exists.
            if (!_scriptDefs.ContainsKey(name))
                return;
            
            // Get the script's definition and prepare a serializer.
            var definition = _scriptDefs[name];
            var serializer = GetSerializerBuilder<SerializerBuilder>().Build();

            // Create the scripts directory to hold our scripts if it doesn't exist.
            if (!Directory.Exists(_config.ScriptsDir))
                Directory.CreateDirectory(_config.ScriptsDir);

            // Serialize the script definition and write it to a file.
            File.WriteAllText(GetScriptFile(definition), serializer.Serialize(definition));
        }

        // Get a list of all known scripts.
        public IScriptMeta[] GetScripts(bool enabledOnly)
            => _scriptDefs.Values.Where(d => enabledOnly ? d.Enabled : true).ToArray();

        // Get the script with the specified name if it exists, otherwise null.
        public IScriptMeta GetScript(string name)
            => _scriptDefs.ContainsKey(name) ? _scriptDefs[name] : null;

        // Return the file path for the script specified.
        private string GetScriptFile(ScriptDefinition d) => $"{_config.ScriptsDir}/{d.Name}.yml";

        private T GetSerializerBuilder<T>() where T : BuilderSkeleton<T>, new()
        {
            // Create a YAML serializer builder.
            T builder = new T()
                .WithNamingConvention(CamelCaseNamingConvention.Instance);

            // The following omits the Type property from IExpression
            // and ensures deserialization will succeed upon loading.
            if (builder is SerializerBuilder serializer)
                serializer.EnsureRoundtrip();

            // Define our tag mappings for the YAML serializer to know the exact
            // type of our IExpressions for deserialization.
            foreach (var tag in _tagMappings)
                builder.WithTagMapping($"tag:yaml.org,2002:{tag.Key}", tag.Value);
            return builder;
        }
    }
}
