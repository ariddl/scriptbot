using DiscordScriptBot.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DiscordScriptBot.Event
{
    public interface IEventInstance
    {
        void Init(params object[] @params);
        object GetParam(string name);
    }

    public class EventInstance<T> : IEventInstance where T : IEvent, new()
    {
        private T _event;
        private Dictionary<string, Func<object>> _eventParams;

        public EventInstance()
        {
            _event = new T();
            _eventParams = new Dictionary<string, Func<object>>();

            foreach (MethodInfo func in typeof(T).GetMethods())
            {
                WrapperDecl attr;
                if ((attr = func.GetCustomAttribute<WrapperDecl>()) == null)
                    continue;
                var expr = Expression.Call(Expression.Constant(_event), func);
                _eventParams.Add(attr.Name, Expression.Lambda<Func<object>>(expr).Compile());
            }
        }

        public void Init(params object[] @params) => _event.Init(@params);

        public object GetParam(string name)
            => _eventParams.ContainsKey(name) ? _eventParams[name]() : null;
    }
}
