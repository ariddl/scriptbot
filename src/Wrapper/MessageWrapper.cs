using Discord;
using DiscordScriptBot.Builder;
using DiscordScriptBot.Utility;
using System.Threading.Tasks;

namespace DiscordScriptBot.Wrapper
{
    [WrapperDecl("message", "A channel message.")]
    public class MessageWrapper : IWrapper
    {
        public IMessage Msg { get; private set; }

        private StringWrapper _text = new StringWrapper();

        //public void Init(object context) => Msg = (IMessage)context;
        public void Init(object context)
        {
            // until we get automatic initializers
            Msg = (IMessage)context;
            _text.Init(Msg.Content);
        }
        public bool InitRef(BuildContext context, CallExpression.ClassRef @ref) => false;
        public LiteralType LiteralsAllowed => LiteralType.None;

        [WrapperDecl("text", "The message content.", Target = "Msg.Content")]
        public StringWrapper Text() => _text;

        [WrapperDecl("test_print", "Test")]
        public async Task Print() => await AtomicConsole.WriteLine($"test_print: {Msg.Content}");
    }
}
