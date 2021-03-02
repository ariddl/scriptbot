using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordScriptBot.Event
{
    public class EventDispatcher
    {
        private DiscordSocketClient _client;

        public EventDispatcher(DiscordSocketClient cl)
        {
            _client = cl;
            cl.UserJoined += UserJoined;
            cl.UserLeft += UserLeft;
            cl.UserUpdated += UserUpdated;
            cl.MessageReceived += MessageReceived;
        }

        private async Task UserUpdated(SocketUser arg1, SocketUser arg2)
        {
            await Task.CompletedTask;
        }

        private async Task UserLeft(SocketGuildUser arg)
        {
            await Task.CompletedTask;
        }

        private async Task UserJoined(SocketGuildUser arg)
        {
            await Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;
            Console.WriteLine(message.Content);
            await Task.CompletedTask;
        }
    }
}
