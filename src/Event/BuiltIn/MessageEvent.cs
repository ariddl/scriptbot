using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Wrapper;

namespace DiscordScriptBot.Event.BuiltIn
{
    [WrapperDecl("message", "Channel message received event")]
    public class MessageEvent : IEvent
    {
        public SocketGuildUser User { get; private set; }
        public SocketGuildChannel Channel { get; private set; }
        public IMessage Msg { get; private set; }

        private MessageWrapper _msg = new MessageWrapper();

        public void Init(params object[] @params)
        {
            SocketMessage msg = (SocketMessage)@params[0];
            User = msg.Author as SocketGuildUser;
            Channel = msg.Channel as SocketGuildChannel;
            Msg = msg;

            _msg.Init(Msg); // temporary until auto-initializers are in
        }

        [WrapperDecl("message", "The message received.", Target="Msg")]
        public MessageWrapper Message() => _msg;
    }
}
