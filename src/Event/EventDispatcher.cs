using Discord.WebSocket;
using DiscordScriptBot.Builder;
using DiscordScriptBot.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordScriptBot.Event
{
    public enum EventType
    {
        Invalid,
        Begin = 1,
        UserJoined = 1,
        UserLeft,
        UserUpdated,
        MessageReceived,
        End
    }

    public class EventDispatcher
    {
        private Config _config;
        private DiscordSocketClient _client;
        private ScriptExecutor _executor;

        private Dictionary<EventType, List<string>> _eventSubscriptions;
        private Dictionary<EventType, Queue<IEventInstance>> _eventPool;
        private static readonly Dictionary<EventType, Type> _eventTypeInstanceMapping
            = new Dictionary<EventType, Type>()
        {
            { EventType.MessageReceived, typeof(EventInstance<MessageEvent>) }
        };

        // TEMPORARY
        public static EventDispatcher Instance { get; private set; }

        public EventDispatcher(Config config, DiscordSocketClient cl, ScriptExecutor exec)
        {
            Instance = this;
            _config = config;
            _client = cl;
            _executor = exec;
            _eventSubscriptions = new Dictionary<EventType, List<string>>();
            _eventPool = new Dictionary<EventType, Queue<IEventInstance>>();

            for (EventType e = EventType.Begin; e < EventType.End; ++e)
            {
                // Check if this event type is supported
                if (!_eventTypeInstanceMapping.ContainsKey(e))
                    continue;
                Type type = _eventTypeInstanceMapping[e];

                _eventPool.Add(e, new Queue<IEventInstance>());
                for (int p = 0; p < _config.EventPoolSize; ++p)
                    _eventPool[e].Enqueue((IEventInstance)Activator.CreateInstance(type));
            }

            _client.UserJoined += async u => await Dispatch(EventType.UserJoined, u);
            _client.UserLeft += async u => await Dispatch(EventType.UserLeft, u);
            _client.UserUpdated += async (a, b) => await Dispatch(EventType.UserUpdated, a, b);
            _client.MessageReceived += async m => await Dispatch(EventType.MessageReceived, m);
        }

        public bool SubscribeScript(string eventName, string scriptName)
        {
            var eventType = GetEventType(GetEventName(eventName));
            if (eventType == EventType.Invalid)
                return false;
            if (!_eventSubscriptions.ContainsKey(eventType))
                _eventSubscriptions.Add(eventType, new List<string>());
            _eventSubscriptions[eventType].Add(scriptName);
            return true;
        }

        public void UnsubscribeScript(string eventName, string scriptName)
            => _eventSubscriptions[GetEventType(GetEventName(eventName))].Remove(scriptName);

        private async Task Dispatch(EventType @event, params object[] @params)
        {
            // Are any scripts listening for this event?
            if (!_eventSubscriptions.ContainsKey(@event))
                return;

            // Try to get an instance of this event type (null if not supported)
            IEventInstance instance = GetEventInstance(@event);
            if (instance == null)
                return;
            instance.Init(@params);

            // This event will be sent to all scripts listening for this event.
            // It holds the event parameters and cannot be modified as such.
            foreach (string script in _eventSubscriptions[@event])
                _executor.EnqueueExecute(script, instance);
            await Task.CompletedTask;
        }

        public IEventInstance GetEventInstance(EventType @event)
        {
            // Check if this event is supported
            if (!_eventPool.ContainsKey(@event))
                return null;

            if (_eventPool[@event].TryDequeue(out IEventInstance instance))
                return instance;
            return (IEventInstance)Activator.CreateInstance(_eventTypeInstanceMapping[@event]);
        }

        public IEventInstance GetEventInstance(string eventName)
            => GetEventInstance(GetEventType(GetEventName(eventName)));

        public void ReturnEventInstance(EventType @event, IEventInstance instance)
        {
            if (!_eventPool.ContainsKey(@event))
                return;
            if (_eventPool[@event].Count < _config.EventPoolSize)
                _eventPool[@event].Enqueue(instance);
        }

        private ExpressionBuilder _test = new ExpressionBuilder();

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Content.StartsWith("~"))
            {
                string[] args = message.Content.Split(' ').Skip(1).ToArray();
                switch (message.Content.Substring(1).ToLower())
                {
                    case "action":
                    {
                        if (args.Length < 2)
                            break;
                        string className = args[1];
                        string funcName = args[2];
                        //_test.Call(className, func, )
                        break;
                    }
                    case "if":
                        break;
                    case "else":
                        break;
                    case "end":
                        break;
                }
            }

            Console.WriteLine(message.Content);
            await Task.CompletedTask;
        }

        private string GetEventName(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);
        private EventType GetEventType(string name) => Enum.TryParse(name, out EventType e) ? e : EventType.Invalid;
    }
}
