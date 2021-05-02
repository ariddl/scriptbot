using Discord.Commands;
using DiscordScriptBot.Builder;
using DiscordScriptBot.Script;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class ScriptBuildCommands : ModuleBase<CommandManager.CommandContext>
    {
        [Group("scriptbuild")]
        public class ScriptBuild : ModuleBase<CommandManager.CommandContext>
        {
            [RequireOwner]
            [Command("new")]
            public async Task NewScript(string name, string eventTrigger)
            {
                var @event = Context.ScriptInterface.GetEvent(eventTrigger);
                if (@event == null)
                {
                    await Context.Reply(nameof(NewScript), "Invalid event name.");
                    return;
                }

                Context.SetContext(new ScriptBuilder(name, null, @event));
                await ReplyAsync($"Now building script: `{name}`, on event: `{eventTrigger}`.");
            }

            [RequireOwner]
            [Command("done")]
            public async Task Done(string option = null)
            {
                var currentScript = Context.GetContext<ScriptBuilder>();
                if (currentScript == null)
                    return;
                Context.ScriptManager.AddScript(currentScript.Name,
                                                currentScript.Description,
                                                Context.Guild.Id,
                                                Context.User.Id.ToString(),
                                                currentScript.SourceEvent.Name,
                                                currentScript.Finish());

                if (option != null && option.ToLower() == "enable")
                    Context.ScriptManager.ActivateScript(currentScript.Name);

                await ReplyAsync($"Script '{currentScript.Name}' created.");
                Context.RemoveContext<ScriptBuilder>();
            }
        }

        [RequireOwner]
        [Command("if")]
        public async Task If(string @class, string func, params string[] @params)
        {

            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().If(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("and")]
        public async Task And(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().And(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("or")]
        public async Task Or(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().Or(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("then")]
        public async Task Then()
        {
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Then();
        }

        [RequireOwner]
        [Command("else")]
        public async Task Else()
        {
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Else();
        }

        [RequireOwner]
        [Command("elif")]
        public async Task ElseIf(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().Else();
            await If(@class, func, @params);
        }

        [RequireOwner]
        [Command("end")]
        public async Task End()
        {
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().End();
        }

        [RequireOwner]
        [Command("action")]
        public async Task Action(string @class, string func, params string[] @params)
        {
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Action(ResolveCall(@class, func, @params));
        }

        private async Task<bool> CheckScriptInProgress()
        {
            if (Context.GetContext<ScriptBuilder>() != null)
                return true;
            await ReplyAsync("No script being built!");
            return false;
        }

        private bool CheckThen(string[] @params, bool doThen)
        {
            if (@params.Length == 0 || @params[@params.Length - 1].ToLower() != "then")
                return false;

            if (doThen)
                Context.GetContext<ScriptBuilder>().Then();
            return true;
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

        private ScriptBuilder.CallInfo ResolveCall(string @class, string func, params string[] @params)
        {
            var @ref = Tokenize(@class, "[", "]");
            bool isRef = @ref.Length == 1;

            var actualParams = new List<IExpression>();
            int offset = CheckThen(@params, false) ? 1 : 0;
            for (int i = 0; i < @params.Length - offset; i++)
                actualParams.Add(ResolveParam(@params[i]));

            return new ScriptBuilder.CallInfo
            {
                Ref = isRef ? @ref[0] : @class,
                ParamName = !isRef ? @class : null,
                ClassName = isRef ? @class.Substring(0, @class.IndexOf('[')) : null,
                FuncName = func,
                FuncParams = actualParams.ToArray()
            };
        }

        private IExpression ResolveParam(string param)
        {
            var call = Tokenize(param, "${", "}");
            if (call.Length == 0)
                return new ConstantExpression { Value = param };

            // Note that these function calls should never have any params since
            // they are likely to just be properties anyway.
            var arg = call[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var info = ResolveCall(arg[0], arg[1]);
            return Context.GetContext<ScriptBuilder>().ResolveCall(info);
        }
    }
}