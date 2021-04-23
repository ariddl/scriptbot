using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using DiscordScriptBot.Wrapper;
using DiscordScriptBot.Event;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;

namespace DiscordScriptBot.Script
{
    public class ScriptInterface
    {
        // Callable function info for the scripting interface.
        public sealed class FunctionInfo
        {
            public MethodInfo Info { get; set; }
            public (string, string)[] Params { get; set; }
            public WrapperDecl Attribute { get; set; }
        }

        // Readonly interface of information about a Discord wrapper.
        public interface IWrapperInfo
        {
            string Name { get; }
            string Description { get; }
            Type Type { get; }
            Dictionary<string, FunctionInfo> Actions { get; }
            Dictionary<string, FunctionInfo> Conditionals { get; }
            Dictionary<string, FunctionInfo> Properties { get; }
        }

        // Internal mutable class representing a Discord wrapper.
        private sealed class WrapperInfo : IWrapperInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Type Type { get; set; }
            public Dictionary<string, FunctionInfo> Actions { get; set; }
            public Dictionary<string, FunctionInfo> Conditionals { get; set; }
            public Dictionary<string, FunctionInfo> Properties { get; set; }
        }

        private Dictionary<string, WrapperInfo> _wrappers;
        private Dictionary<string, WrapperInfo> _events;
        private Dictionary<Type, WrapperInfo> _wrapperTypes;

        public ScriptInterface()
        {
            _wrappers = new Dictionary<string, WrapperInfo>();
            _events = new Dictionary<string, WrapperInfo>();
            _wrapperTypes = new Dictionary<Type, WrapperInfo>();

            // Iterate over all of our loaded assemblies to find event/wrapper types.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Iterate over all the types in the current assembly.
                foreach (Type type in assembly.GetTypes())
                    LoadType(type);
            }
        }

        private void LoadType(Type type)
        {
            // Event/wrapper types must be marked by Wrapper attributes.
            WrapperDecl wrapAttr;
            if ((wrapAttr = type.GetCustomAttribute<WrapperDecl>()) == null)
                return;

            // Check if the type implements either the IWrapper or IEvent interfaces (required).
            var interfaces = type.GetInterfaces();
            if (!interfaces.Contains(typeof(IWrapper)) && !interfaces.Contains(typeof(IEvent)))
                return;

            // Events are marked by their implementation of IEvent. Choose the appropriate
            // dictionary depending on if this is a wrapper or event type.
            bool isEvent = interfaces.Contains(typeof(IEvent));
            var wrapDict = !isEvent ? _wrappers : _events;
            Debug.Assert(!wrapDict.ContainsKey(wrapAttr.Name),
                $"Duplicate Wrapper Type: {type.Name} Name: {wrapAttr.Name}");

            // Initialize the wrapper info to hold information about the APIs exposed by this type.
            var wrapper = new WrapperInfo
            {
                Name = wrapAttr.Name,
                Description = wrapAttr.Description,
                Type = type,
                Actions = new Dictionary<string, FunctionInfo>(),
                Conditionals = new Dictionary<string, FunctionInfo>(),
                Properties = new Dictionary<string, FunctionInfo>()
            };
            wrapDict.Add(wrapAttr.Name, wrapper);
            _wrapperTypes.Add(type, wrapper);

            // Iterate over the members of the type to find the API functions exposed by this type.
            foreach (MemberInfo m in type.GetMembers())
            {
                // Each API member must be marked by a WrapperDecl, similar to the type itself.
                WrapperDecl memberAttr;
                if ((memberAttr = m.GetCustomAttribute<WrapperDecl>()) == null)
                    continue;

                // Support only methods for now. We may want to explore supporting properties as well...
                Debug.Assert(m.MemberType == MemberTypes.Method,
                    $"Interface member is not a method: {memberAttr.Name}");
                MethodInfo method = (MethodInfo)m;

                // Choose the appropriate dictionary to store this function
                var rt = method.ReturnType;
                var dict = wrapper.Properties;
                if (rt == typeof(bool))
                    dict = wrapper.Conditionals;
                else if (rt == typeof(void) || rt == typeof(Task))
                    dict = wrapper.Actions;

                Debug.Assert(!isEvent || dict != wrapper.Conditionals, "Event has conditional!");
                Debug.Assert(!dict.ContainsKey(memberAttr.Name),
                    $"Duplicate interface member: {memberAttr.Name}");

                // Build the function info and function parameter dictionary.
                var parameters = new List<(string, string)>();
                foreach (ParameterInfo parameter in method.GetParameters())
                    parameters.Add((parameter.Name, parameter.ParameterType.Name));
                dict.Add(memberAttr.Name, new FunctionInfo
                {
                    Info = method,
                    Params = parameters.ToArray(),
                    Attribute = memberAttr
                });
            }
        }

        public IWrapperInfo[] GetWrappers() => _wrappers.Values.ToArray();
        public IWrapperInfo[] GetEvents() => _events.Values.ToArray();

        public IWrapperInfo GetWrapper(string name) => _wrappers.ContainsKey(name) ? _wrappers[name] : null;
        public IWrapperInfo GetWrapper(Type type) => _wrapperTypes.ContainsKey(type) ? _wrapperTypes[type] : null;
        public IWrapperInfo GetEvent(string name) => _events.ContainsKey(name) ? _events[name] : null;

        public static string GetInterfaceString(IWrapperInfo wrapper, string type)
        {
            var b = new StringBuilder();
            b.AppendLine($"`{type}({wrapper.Name})`: {wrapper.Description}```");
            AppendFunctions(b, "Actions", wrapper.Actions);
            AppendFunctions(b, "Conditionals", wrapper.Conditionals);
            AppendFunctions(b, "Properties", wrapper.Properties);
            b.AppendLine("```");
            return b.ToString();
        }

        private static void AppendFunctions(StringBuilder b, string title, Dictionary<string, FunctionInfo> dict)
        {
            if (dict.Count == 0)
                return;
            b.AppendLine($" {title}:");
            foreach (var keyval in dict)
            {
                b.AppendLine($" - {keyval.Key}: {keyval.Value.Attribute.Description}");
                if (keyval.Value.Params.Length == 0)
                    continue;
                b.AppendLine("   Params: ");
                foreach ((string name, string type) @param in keyval.Value.Params)
                    b.AppendLine($"   - {@param.name}: {@param.type}");
            }
        }
    }
}
