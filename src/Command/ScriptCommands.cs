using Discord.Commands;
using System.Text;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class ScriptCommands : ModuleBase<CommandManager.CommandContext>
    {
        [RequireOwner]
        [Command("showscript")]
        public async Task ShowScript(string name = null)
        {
            // Name is optional.
            if (name == null)
            {
                // Not looking for a specific script; list them all.
                var b = new StringBuilder("```");
                foreach (var script in Context.ScriptManager.GetScripts(false))
                    b.AppendLine($"{script.Name}: {script.Description} [{(script.Enabled ? "ENABLED" : "DISABLED")}]");
                b.AppendLine("```");
                await ReplyAsync(b.ToString());
                return;
            }

            // Looking for a specific script.
            {
                // Try to find the script with the name given.
                var script = Context.ScriptManager.GetScript(name.ToLower());
                if (script != null)
                {
                    // Reply with the script's meta data.
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

        [RequireOwner]
        [Command("enablescript")]
        public async Task EnableScript(string name)
        {
            // Attempt to enable the script with the name provided.
            bool ok = Context.ScriptManager.SetScriptEnabled(name.ToLower(), true, true);
            await Context.Reply(nameof(EnableScript),
                ok ? $"'{name}' has been enabled." : $"'{name}' could not be enabled.");
        }

        [RequireOwner]
        [Command("disablescript")]
        public async Task DisableScript(string name)
        {
            // Attempt to disable the script with the name provided.
            bool ok = Context.ScriptManager.SetScriptEnabled(name.ToLower(), false, true);
            await Context.Reply(nameof(DisableScript),
                ok ? $"'{name}' has been disabled." : $"'{name}' could not be disabled.");
        }
    }
}