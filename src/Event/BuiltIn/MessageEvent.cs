using Discord;
using Discord.WebSocket;
using DiscordScriptBot.Wrapper;

namespace DiscordScriptBot.Event.BuiltIn
{
    [WrapperDecl("message", "Channel message received event")]
    public class MessageEvent : IEvent
    {
        public IGuildUser User { get; private set; }
        public IMessageChannel Channel { get; private set; }
        public IMessage Msg { get; private set; }

        private MessageWrapper _msg = new MessageWrapper();
        private TextChannelWrapper _channel = new TextChannelWrapper();

        public void Init(params object[] @params)
        {
            SocketMessage msg = (SocketMessage)@params[0];
            User = msg.Author as SocketGuildUser;
            Channel = msg.Channel;
            Msg = msg;

            _msg.Init(Msg); // temporary until auto-initializers are in
            _channel.Init(Channel); // temporary until auto-initializers are in
        }

        [WrapperDecl("message", "The message received.", Target="Msg")]
        public MessageWrapper Message() => _msg;

        [WrapperDecl("textChannel", "The channel that this message was sent in.")]
        public TextChannelWrapper TextChannel() => _channel;
    }
}
