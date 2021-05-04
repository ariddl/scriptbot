using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordScriptBot.Script;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class CommandManager
    {
        // We use a custom command context so that we can quickly access our
        // managers without the need of a singleton. This also gives us the
        // opportunity to introduce utility functions (such as Reply() below).
        public class CommandContext : SocketCommandContext
        {
            private CommandManager _commandManager;

            // Our context dictionary for quick access to instances for
            // this user (i.e., a ScriptBuilder).
            private Dictionary<Type, object> Ctx
                => _commandManager._userContext.ContainsKey(User.Id) ? 
                   _commandManager._userContext[User.Id] : null;

            // Quick references for commands to use (no singletons currently!)
            public ScriptManager ScriptManager => _commandManager._scriptManager;
            public ScriptInterface ScriptInterface => _commandManager._scriptInterface;

            public CommandContext(CommandManager manager,
                                  DiscordSocketClient client, SocketUserMessage msg)
                : base(client, msg)
            {
                _commandManager = manager;
            }

            public void SetContext<T>(T value)
            {
                // Check if we have a context dictionary for this user.
                // If not, create one.
                if (Ctx == null)
                    _commandManager._userContext.Add(User.Id, new Dictionary<Type, object>());

                // If we don't already have context for this type, add it.
                // Otherwise, overwrite it with the new instance.
                if (GetContext<T>() == null)
                    Ctx.Add(typeof(T), value);
                else
                    Ctx[typeof(T)] = value;
            }

            public void RemoveContext<T>()
            {
                // Do we have context for this type at all?
                if (GetContext<T>() == null)
                    return;

                // We have an instance of this type for this user, remove it!
                // If this is the last element in our dictionary, remove it
                // from the user context dictionary as well.
                Ctx.Remove(typeof(T));
                if (Ctx.Count == 0)
                    _commandManager._userContext.Remove(User.Id);
            }

            // Try to get an instance of the type specified for this user.
            // If one doesn't exist, return the default (which is typically
            // a null reference).
            public T GetContext<T>() => Ctx != null ?
              (Ctx.ContainsKey(typeof(T)) ? 
              (T)Ctx[typeof(T)] : default(T)) : default(T);

            public async Task Reply(string cmd, string msg)
                => await Channel.SendMessageAsync($"`{cmd}`: {msg}");
        }

        // The character that every command must start with.
        private const char PrefixChar = '/';

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private ScriptManager _scriptManager;
        private ScriptInterface _scriptInterface;
        private Dictionary<ulong, Dictionary<Type, object>> _userContext;

        public CommandManager(DiscordSocketClient client, ScriptManager manager, ScriptInterface @interface)
        {
            // Setup command service.
            _client = client;
            _commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true
            });
            _scriptManager = manager;
            _scriptInterface = @interface;
            _userContext = new Dictionary<ulong, Dictionary<Type, object>>();

            // Subscrive events.
            _client.MessageReceived += MessageReceived;
            _commands.CommandExecuted += CommandExecuted;
        }

        public async Task InitAsync()
        {
            // Add command modules from ALL of our assemblies
            // Note: Probably want to switch to an IServiceProvider for this.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                await _commands.AddModulesAsync(assembly, null);
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // User messages only! I.e., ignore system messages and messages from other bots.
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // Check for our command prefix.
            var argPos = 0;
            if (!message.HasCharPrefix(PrefixChar, ref argPos)) return;

            var context = new CommandContext(this, _client, message);
            await _commands.ExecuteAsync(context, argPos, null);
        }

        private async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // Command is unspecified upon search failure (unknown command); ignore.
            if (!command.IsSpecified)
                return;

            // Check if the command completed successfully, otherwise send message with the error.
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync($"The command could not be completed: {result}");
        }
    }
}
