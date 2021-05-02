using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Command
{
    [Group("interface")]
    public class ListInterfaceCommands : ModuleBase<CommandManager.CommandContext>
    {
        [Group("describe")]
        public class Describe : ModuleBase<CommandManager.CommandContext>
        {
            [RequireOwner]
            [Command("event")]
            public async Task ShowEvent(string name = null)
            {
                if (name == null)
                {
                    // If no name provided, show all events
                    await ShowAll(Context.ScriptInterface.GetEvents());
                    return;
                }

                var @event = Context.ScriptInterface.GetEvent(name);
                if (@event != null)
                    await ReplyAsync(GetInterfaceString(@event, "Event"));
                else
                    await Context.Reply(nameof(ShowEvent), $"No event found for '{name}'.");
            }

            [RequireOwner]
            [Command("object")]
            public async Task ShowObject(string name = null)
            {
                if (name == null)
                {
                    // If no name provided, show all objects/wrappers
                    await ShowAll(Context.ScriptInterface.GetWrappers());
                    return;
                }

                var wrapper = Context.ScriptInterface.GetWrapper(name);
                if (wrapper != null)
                    await ReplyAsync(GetInterfaceString(wrapper, "Object"));
                else
                    await Context.Reply(nameof(ShowObject), $"No object found for '{name}'.");
            }

            private async Task ShowAll(IWrapperInfo[] wrappers)
            {
                var b = new StringBuilder();
                b.AppendLine("```");
                foreach (IWrapperInfo w in wrappers)
                    b.AppendLine($"{w.Name}: {w.Description}");
                b.AppendLine("```");
                await ReplyAsync(b.ToString());
            }
        }
    }
}
