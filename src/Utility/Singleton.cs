namespace DiscordScriptBot.Utility
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;

        public static T Instance => _instance ?? (_instance = new T());
    }
}
