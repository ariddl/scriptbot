﻿using Discord.Commands;
using System.Text;
using System.Threading.Tasks;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Command
{
    public class ListInterfaceCommand : ModuleBase<CommandManager.CommandContext>
    {
        [Command("showevent")]
        public async Task ShowEvent(string name = null)
        {
            if (name == null)
            {
                // If no name provided, show all events
                await ShowAll(Context.ScriptInterface.GetEvents());
                return;
            }

            var @event = Context.ScriptInterface.GetEvent(name.ToLower());
            if (@event != null)
                await Context.Reply(GetInterfaceString(@event, "Event"));
            else
                await Context.Reply("showevent", $"No event found for '{name}'.");
        }

        [Command("showobj")]
        public async Task ShowObject(string name = null)
        {
            if (name == null)
            {
                // If no name provided, show all objects/wrappers
                await ShowAll(Context.ScriptInterface.GetWrappers());
                return;
            }

            var wrapper = Context.ScriptInterface.GetWrapper(name.ToLower());
            if (wrapper != null)
                await Context.Reply(GetInterfaceString(wrapper, "Object"));
            else
                await Context.Reply("showobj", $"No object found for '{name}'.");
        }

        private async Task ShowAll(IWrapperInfo[] wrappers)
        {
            var b = new StringBuilder();
            b.AppendLine("```");
            foreach (IWrapperInfo w in wrappers)
                b.AppendLine($"{w.Name}: {w.Description}");
            b.AppendLine("```");
            await Context.Reply(b.ToString());
        }
    }
}
