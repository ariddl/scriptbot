using Discord.Commands;
using DiscordScriptBot.Script;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class ScriptBuildCommands : ModuleBase<CommandManager.CommandContext>
    {
        [Command("newscript")]
        public async Task NewScript(string name, string eventTrigger)
        {
            await Task.CompletedTask; // TODO
        }

        [Command("if")]
        public async Task If(string expr, string then = null)
        {
            var arg = expr.Split(' ');
            var @object = arg[0];
            var @ref = Tokenize(@object, "[", "]");

            //b.SourceEvent.Properties.ContainsKey()
            await Task.CompletedTask; // TODO
        }

        [Command("then")]
        public async Task Then()
        {
            await Task.CompletedTask; // TODO
        }

        [Command("else")]
        public async Task Else()
        {
            await Task.CompletedTask; // TODO
        }

        [Command("elif")]
        public async Task ElseIf()
        {
            var b = new ScriptBuilder(null, null, null);
            b.Else();
            b.If();
            await Task.CompletedTask; // TODO
        }

        [Command("end")]
        public async Task End()
        {
            //b.Pop();
            await Task.CompletedTask; // TODO
        }

        [Command("action")]
        public async Task Action(string stmt)
        {
            await Task.CompletedTask; // TODO
        }

        [Command("done")]
        public async Task Done(string option = null)
        {
            // option=="enable" then enable script immediately
            await Task.CompletedTask; // TODO
        }

        private static string[] Tokenize(string src, string start, string end)
        {
            // TODO: More robust parsing?
            var list = new List<string>();
            int sIndex, eIndex;
            while ((sIndex = src.IndexOf(start)) >= 0 && (eIndex = src.IndexOf(end, sIndex)) >= 0)
            {
                sIndex += start.Length;
                list.Add(src.Substring(sIndex, eIndex - sIndex));
                src = src.Substring(eIndex + end.Length);
            }
            return list.ToArray();
        }
    }
}