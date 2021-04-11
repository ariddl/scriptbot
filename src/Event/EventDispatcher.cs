using Discord.WebSocket;
using DiscordScriptBot.Event.BuiltIn;
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
        UserIsTyping,
        UserBanned,
        UserUnbanned,
        UserVoiceState,
        MessageReceived,
        MessageUpdated,
        MessageDeleted,
        MessagesBulkDeleted,
        ReactionAdded,
        ReactionRemoved,
        RoleCreated,
        RoleDeleted,
        RoleUpdated,
        InviteCreated,
        InviteDeleted,
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
            // TODO: Better system for this.
            { EventType.MessageReceived, (d, p) => (p[0] is SocketMessage m) && (m.Author.Id == d._client.CurrentUser.Id || m.Content.StartsWith("/")) }
        };

        private DiscordSocketClient _client;
        private ScriptExecutor _executor;
        private Dictionary<EventType, List<string>> _eventSubscriptions;

        public EventDispatcher(DiscordSocketClient cl, ScriptExecutor exec)
        {
            _client = cl;
            _executor = exec;
            _eventSubscriptions = new Dictionary<EventType, List<string>>();

            // TODO: Better system for this.
            _client.UserJoined += async u => await Dispatch(EventType.UserJoined, u);
            _client.UserLeft += async u => await Dispatch(EventType.UserLeft, u);
            _client.UserUpdated += async (a, b) => await Dispatch(EventType.UserUpdated, a, b);
            _client.UserIsTyping += async (u, c) => await Dispatch(EventType.UserIsTyping, u, c);
            _client.UserBanned += async (u, g) => await Dispatch(EventType.UserBanned, u, g);
            _client.UserUnbanned += async (u, g) => await Dispatch(EventType.UserUnbanned, u, g);
            _client.UserVoiceStateUpdated += async (u, s1, s2) => await Dispatch(EventType.UserVoiceState, u, s1, s2);
            _client.MessageReceived += async m => await Dispatch(EventType.MessageReceived, m);
            _client.MessageUpdated += async (sm, m, u) => await Dispatch(EventType.MessageUpdated, sm, m, u);
            _client.MessageDeleted += async (m, c) => await Dispatch(EventType.MessageDeleted, m, c);
            _client.MessagesBulkDeleted += async (m, c) => await Dispatch(EventType.MessagesBulkDeleted, m, c);
            _client.ReactionAdded += async (m, c, r) => await Dispatch(EventType.ReactionAdded, m, c, r);
            _client.ReactionRemoved += async (m, c, r) => await Dispatch(EventType.ReactionRemoved, m, c, r);
            _client.RoleCreated += async r => await Dispatch(EventType.RoleCreated, r);
            _client.RoleDeleted += async r => await Dispatch(EventType.RoleDeleted, r);
            _client.RoleUpdated += async (r1, r2) => await Dispatch(EventType.RoleUpdated, r1, r2);
            _client.InviteCreated += async i => await Dispatch(EventType.InviteCreated, i);
            _client.InviteDeleted += async (c, i) => await Dispatch(EventType.InviteDeleted, c, i);
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
