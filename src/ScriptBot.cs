using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Event;
using DiscordScriptBot.Script;

namespace DiscordScriptBot
{
    public class ScriptBot
    {
        private Config _config;
        private readonly DiscordSocketClient _client;

        private ScriptManager _scriptManager;
        private ScriptInterface _scriptInterface;
        private ScriptExecutor _scriptExecutor;
        private EventDispatcher _eventDispatcher;

        public ScriptBot(Config config)
        {
            _config = config;
            _client = new DiscordSocketClient();
            _client.Ready += Ready;
            _client.Log += Log;
        }

        private Task Ready()
        {
            // Connected to Discord, let's initialize!
            _scriptInterface = new ScriptInterface();
            _scriptExecutor = new ScriptExecutor(_config, _client, _scriptInterface);
            _eventDispatcher = new EventDispatcher(_client, _scriptExecutor);
            _scriptManager = new ScriptManager(_config, _scriptExecutor, _eventDispatcher);

            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine($"Log: {arg.ToString()}");
            return Task.CompletedTask;
        }

        public async Task Run()
        {
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }
    }
}
