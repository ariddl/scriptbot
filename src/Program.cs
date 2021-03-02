using System;
using System.Threading.Tasks;

namespace DiscordScriptBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string configFile = args.Length > 1 ? args[1] : "config.yml";
            
            Config config = Config.Load(configFile);
            if (!config.Validate())
            {
                Console.WriteLine($"Configuration file {configFile} is invalid!");
                return;
            }

            ScriptBot bot = new ScriptBot(config);
            await bot.Run();
        }
    }
}
