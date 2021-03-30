using Discord;
using DiscordScriptBot.Builder;
using System.Linq;

namespace DiscordScriptBot.Wrapper
{
    public class ChannelWrapper : IWrapper
    {
        private IChannel _channel;

        public void Init(object context) => _channel = (IChannel)context;

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

        public LiteralType LiteralsAllowed => LiteralType.Int | LiteralType.String;
    }
}
