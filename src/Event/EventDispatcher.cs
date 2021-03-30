using Discord.WebSocket;
using DiscordScriptBot.Script;
using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<EventType, Type> _eventTypes = new Dictionary<EventType, Type>()
        {
            { EventType.MessageReceived, typeof(EventInstance<MessageEvent>) }
        };

        private delegate bool EventFilter(EventDispatcher d, params object[] @params);
        private static readonly Dictionary<EventType, EventFilter> _eventFilters = new Dictionary<EventType, EventFilter>()
        {
            { EventType.MessageReceived, (d, p) => (p[0] as SocketMessage).Author.Id == d._client.CurrentUser.Id }
        };

        private DiscordSocketClient _client;
        private ScriptExecutor _executor;
        private Dictionary<EventType, List<string>> _eventSubscriptions;

        public EventDispatcher(DiscordSocketClient cl, ScriptExecutor exec)
        {
            _client = cl;
            _executor = exec;
            _eventSubscriptions = new Dictionary<EventType, List<string>>();

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

            // Check if we should skip this event via filter
            if (_eventFilters.ContainsKey(@event) && _eventFilters[@event](this, @params))
                return;

            // This event will be sent to all scripts listening for this event.
            // It holds the event parameters and cannot be modified as such.
            foreach (string script in _eventSubscriptions[@event])
                _executor.EnqueueExecute(script, @params);
            await Task.CompletedTask;
        }

        public static IEventInstance CreateEventInstance(string eventName)
        {
            // Check if this event is supported
            var eventType = GetEventType(GetEventName(eventName));
            if (eventType == EventType.Invalid || !_eventTypes.ContainsKey(eventType))
                return null;
            return (IEventInstance)Activator.CreateInstance(_eventTypes[eventType]);
        }

        private static string GetEventName(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);
        private static EventType GetEventType(string name) => Enum.TryParse(name, out EventType e) ? e : EventType.Invalid;
    }
}
