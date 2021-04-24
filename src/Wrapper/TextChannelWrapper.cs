using Discord;
using DiscordScriptBot.Builder;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordScriptBot.Wrapper
{
    [WrapperDecl("textChannel", "A text channel.")]
    public class TextChannelWrapper : IWrapper
    {
        private IMessageChannel _channel;

        public void Init(object context) => _channel = (IMessageChannel)context;

        public bool InitRef(BuildContext context, CallExpression.ClassRef @ref)
        {
            switch (@ref.RefType)
            {
                case CallExpression.ClassRef.TypeId:
                    if (ulong.TryParse(@ref.Value, out ulong id))
                        Init(context.Guild.GetChannel(id));
                    break;
                case CallExpression.ClassRef.TypeStr:
                    var chan = context.Guild.Channels.FirstOrDefault(c => c.Name == @ref.Value);
                    if (chan != null)
                        Init(chan);
                    break;
            }
            return _channel != null;
        }

        [WrapperDecl("id", "The channel id.")]
        public ulong Id() => _channel.Id;

        [WrapperDecl("name", "The channel name.")] // TODO StringWrapper
        public string Name() => _channel.Name;

        [WrapperDecl("sendText", "Send a text message to this channel.")]
        public async Task SendText(string text) => await _channel.SendMessageAsync(text);

        public LiteralType LiteralsAllowed => LiteralType.Int | LiteralType.String;
    }
}
