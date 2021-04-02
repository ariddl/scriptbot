using Discord.Commands;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Command
{
    public class ListInterfaceCommand : ModuleBase<CommandManager.CommandContext>
    {
        [Command("events")]
        public async Task ListEvents()
        {
            var events = Context.ScriptInterface.GetEvents();
            var reply = GetWrappersString("Events", events);
            await Context.Channel.SendMessageAsync(reply);
        }

        [Command("wrappers")]
        public async Task ListWrappers()
        {
            var wrappers = Context.ScriptInterface.GetWrappers();
            var reply = GetWrappersString("Wrappers", wrappers);
            await Context.Channel.SendMessageAsync(reply);
        }

        private string GetWrappersString(string title, IWrapperInfo[] wrappers)
        {
            if (wrappers.Length == 0)
                return string.Empty;
            var b = new StringBuilder();
            b.AppendLine($"{title}:");
            foreach (IWrapperInfo wrapper in wrappers)
            {
                b.AppendLine("```");
                b.AppendLine($"{wrapper.Name}: {wrapper.Description}");
                b.AppendLine(GetFunctionStrings("Actions", wrapper.Actions));
                b.AppendLine(GetFunctionStrings("Conditionals", wrapper.Conditionals));
                b.AppendLine(GetFunctionStrings("Properties", wrapper.Properties));
                b.AppendLine("```");
            }
            return b.ToString();
        }

        private string GetFunctionStrings(string title, Dictionary<string, FunctionInfo> dict)
        {
            var b = new StringBuilder();
            b.AppendLine($" {title}:");
            foreach (var keyval in dict)
            {
                // Temporary; TODO: Add IWrapperInfo to FunctionInfo to get this info
                b.AppendLine($" - {keyval.Key}: {keyval.Value}");
                if (keyval.Value.Params.Length == 0)
                    continue;
                b.AppendLine($" Params: ");
                foreach ((string name, string type) @param in keyval.Value.Params)
                    b.AppendLine($"  - ${@param.name}: {@param.type}");
            }
            return b.ToString();
        }
    }
}
