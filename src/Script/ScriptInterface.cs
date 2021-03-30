using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using DiscordScriptBot.Wrapper;
using DiscordScriptBot.Event;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiscordScriptBot.Script
{
    public class ScriptInterface
    {
        public sealed class FunctionInfo
        {
            public MethodInfo Info;
            public (string, string)[] Params;
        }

        public interface IWrapperInfo
        {
            string Name { get; }
            string Description { get; }
            Type Type { get; }
            Dictionary<string, FunctionInfo> Actions { get; }
            Dictionary<string, FunctionInfo> Conditionals { get; }
            Dictionary<string, FunctionInfo> Properties { get; }
        }

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

        public ScriptInterface()
        {
            _wrappers = new Dictionary<string, WrapperInfo>();
            _events = new Dictionary<string, WrapperInfo>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    WrapperDecl wrapAttr;
                    if ((wrapAttr = type.GetCustomAttribute<WrapperDecl>()) == null)
                        continue;

                    var interfaces = type.GetInterfaces();
                    if (!interfaces.Contains(typeof(IWrapper)) && !interfaces.Contains(typeof(IEvent)))
                        continue;

                    bool isEvent = interfaces.Contains(typeof(IEvent));
                    var wrapDict = !isEvent ? _wrappers : _events;
                    Debug.Assert(!wrapDict.ContainsKey(wrapAttr.Name),
                        $"Duplicate Wrapper Type: {type.Name} Name: {wrapAttr.Name}");

                    var wrapper = new WrapperInfo()
                    {
                        Name = wrapAttr.Name,
                        Description = wrapAttr.Description,
                        Type = type,
                        Actions = new Dictionary<string, FunctionInfo>(),
                        Conditionals = new Dictionary<string, FunctionInfo>(),
                        Properties = new Dictionary<string, FunctionInfo>()
                    };
                    wrapDict.Add(wrapAttr.Name, wrapper);

                    foreach (MemberInfo m in type.GetMembers())
                    {
                        WrapperDecl memberAttr;
                        if ((memberAttr = m.GetCustomAttribute<WrapperDecl>()) == null)
                            continue;

                        // Support only methods for now. We may want to explore supporting properties as well...
                        Debug.Assert(m.MemberType == MemberTypes.Method,
                            $"Interface member is not a method: {memberAttr.Name}");
                        MethodInfo method = (MethodInfo)m;

                        // Choose the appropriate dictionary to store this function
                        var dict = wrapper.Properties;
                        if (method.ReturnType == typeof(bool))
                            dict = wrapper.Conditionals;
                        else if (method.ReturnType == typeof(void) || method.ReturnType == typeof(Task))
                            dict = wrapper.Actions;

                        Debug.Assert(!isEvent || dict != wrapper.Conditionals, "Event has conditional!");
                        Debug.Assert(!dict.ContainsKey(memberAttr.Name),
                            $"Duplicate interface member: {memberAttr.Name}");

                        var parameters = new List<(string, string)>();
                        foreach (ParameterInfo parameter in method.GetParameters())
                            parameters.Add((parameter.Name, parameter.ParameterType.Name));
                        dict.Add(memberAttr.Name, new FunctionInfo
                        {
                            Info = method,
                            Params = parameters.ToArray()
                        });
                    }
                }
            }
        }

        public IWrapperInfo[] GetWrappers() => _wrappers.Values.ToArray();
        public IWrapperInfo[] GetEvents() => _events.Values.ToArray();

        public IWrapperInfo GetWrapper(string name) => _wrappers.ContainsKey(name) ? _wrappers[name] : null;
        public IWrapperInfo GetEvent(string name) => _events.ContainsKey(name) ? _events[name] : null;
    }
}
