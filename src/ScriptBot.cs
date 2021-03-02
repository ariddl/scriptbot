using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Event;
using DiscordScriptBot.Expression;

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
            var e = new InfixExpression(new WrappedEvaluable(() => true));
            e.And(new WrappedEvaluable(() => false))
             .Or(new WrappedEvaluable(() => true))
             .And(new WrappedEvaluable(() => true))
             .Xor(new WrappedEvaluable(() => false));
            Console.WriteLine($"Expr test: {e} -> {e.Evaluate()}");
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
