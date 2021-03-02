using Discord;

namespace DiscordScriptBot.Element
{
    public class MessageChannelElement : IElement
    {
        public string ElementName => "channel";
        private IMessageChannel _chan;

        public MessageChannelElement(IMessageChannel chan)
        {
            _chan = chan;
        }

        [ElementInterface("name", "The name of the channel.")]
        public StringElement Name() => new StringElement(_chan.Name);

        [ElementInterface("sendText", "Send a text message on the channel.")]
        public async void SendText(string text, bool tts = false) => await _chan.SendMessageAsync(text, tts);

        [ElementInterface("sendFile", "Send a file on the channel.")]
        public async void SendFile(string filePath, string text = null, bool tts = false) =>
            await _chan.SendFileAsync(filePath, text, tts);
    }
}
