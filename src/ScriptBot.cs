using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Event;

namespace DiscordScriptBot
{
    public class ScriptBot
    {
        private Config _config;
        private readonly DiscordSocketClient _client;
        private EventDispatcher _dispatcher;

        public ScriptBot(Config config)
        {
            _config = config;
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _dispatcher = new EventDispatcher(_client);

            // Temp test stuff
            Expression.Test.RunTests();
            Script.Test.RunTests();
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
