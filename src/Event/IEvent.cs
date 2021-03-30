namespace DiscordScriptBot.Event
{
    public interface IEvent
    {
        void Init(params object[] @params);
    }
}
