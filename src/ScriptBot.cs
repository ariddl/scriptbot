using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Command;
using DiscordScriptBot.Event;
using DiscordScriptBot.Script;
using DiscordScriptBot.Utility;

namespace DiscordScriptBot
{
    public class ScriptBot
    {
        private Config _config;
        private readonly DiscordSocketClient _client;
        private SemaphoreSlim _signal;

        private ScriptManager _scriptManager;
        private ScriptInterface _scriptInterface;
        private ScriptExecutor _scriptExecutor;
        private EventDispatcher _eventDispatcher;
        private CommandManager _commandManager;

        public ScriptBot(Config config)
        {
            _config = config;
            _client = new DiscordSocketClient();
            _client.Ready += Ready;
            _client.Disconnected += Disconnected;
            _client.Log += Log;
            _signal = new SemaphoreSlim(0, 1);
        }

        private async Task Ready()
        {
            // Connected to Discord, let's initialize!
            _scriptInterface = new ScriptInterface();
            _scriptExecutor = new ScriptExecutor(_config, _client, _scriptInterface);
            _eventDispatcher = new EventDispatcher(_client, _scriptExecutor);
            _scriptManager = new ScriptManager(_config, _scriptExecutor, _eventDispatcher);
            _commandManager = new CommandManager(_client, _scriptManager, _scriptInterface);
            await _commandManager.InitAsync();

            await AtomicConsole.WriteLine("ScriptBot Ready.");
        }
        
        private async Task Disconnected(Exception arg)
        {
            await AtomicConsole.WriteLine("Disconnected from Discord; shutting down.");
            _scriptExecutor.Stop();
            _signal.Release();
        }

        private async Task Log(LogMessage arg)
        {
            if (_config.LogDiscord)
                await AtomicConsole.WriteLine($"Log: {arg.ToString()}");
        }

        public async Task Run()
        {
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            // Run until we receive shutdown signal
            await _signal.WaitAsync();
        }
    }
}
