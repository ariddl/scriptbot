using Discord.Commands;
using System.Text;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class ScriptCommands : ModuleBase<CommandManager.CommandContext>
    {
        [Command("showscript")]
        public async Task ShowScript(string name = null)
        {
            if (name == null)
            {
                var b = new StringBuilder("```");
                foreach (var script in Context.ScriptManager.GetScripts(false))
                    b.AppendLine($"{script.Name}: {script.Description} [{(script.Enabled ? "ENABLED" : "DISABLED")}]");
                b.AppendLine("```");
                await ReplyAsync(b.ToString());
                return;
            }
            {
                var script = Context.ScriptManager.GetScript(name.ToLower());
                if (script != null)
                {
                    await ReplyAsync($"`{script.Name}: {script.Description}` ```" +
                                     $"Guild/server: {script.Guild}\n" +
                                     $"EventTrigger: {script.EventTrigger}\n" +
                                     $"Author: {script.Author}\n" +
                                     $"Created: {script.CreationDate.ToString("MM/dd/yyyy HH:mm:ss")}\n" +
                                     $"Enabled: {script.Enabled}```");
                }
                else
                    await Context.Reply("showscript", "Script not found.");
            }
        }

        [Command("enablescript")]
        public async Task EnableScript(string name)
        {
            bool ok = Context.ScriptManager.SetScriptEnabled(name.ToLower(), true, true);
            await Context.Reply("enablescript",
                ok ? $"'{name}' has been enabled." : $"'{name}' could not be enabled.");
        }

        [Command("disablescript")]
        public async Task DisableScript(string name)
        {
            bool ok = Context.ScriptManager.SetScriptEnabled(name.ToLower(), false, true);
            await Context.Reply("disablescript",
                ok ? $"'{name}' has been disabled." : $"'{name}' could not be disabled.");
        }
    }
}