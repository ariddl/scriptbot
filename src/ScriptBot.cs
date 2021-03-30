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
        private InterfaceManager _scriptInterface;
        private ScriptExecutor _scriptExecutor;
        private EventDispatcher _dispatcher;

        public ScriptBot(Config config)
        {
            _config = config;
            _client = new DiscordSocketClient();
            _client.Ready += Ready;
            _client.Log += Log;

            _scriptManager = new ScriptManager(_config);
            _scriptInterface = new InterfaceManager();
            _scriptExecutor = new ScriptExecutor(_config, _client, _scriptInterface, _scriptManager);
            _dispatcher = new EventDispatcher(_config, _client, _scriptExecutor);
            _scriptExecutor.Load();

            //_scriptManager.RemoveScript("test");
            ////return;
            //_scriptManager.AddScript("test", "test script", "author", new Builder.BlockExpression
            //{
            //    Expressions = new System.Collections.Generic.List<Builder.IExpression>()
            //});
        }

        private Task Ready()
        {
            Console.WriteLine("ScriptBot ready");
            // activate scripts
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
