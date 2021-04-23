﻿using Discord.Commands;
using DiscordScriptBot.Builder;
using DiscordScriptBot.Script;
using DiscordScriptBot.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordScriptBot.Command
{
    public class ScriptBuildCommands : ModuleBase<CommandManager.CommandContext>
    {
        private static ScriptBuilder _currentScript;

        [Command("newscript")]
        public async Task NewScript(string name, string eventTrigger)
        {
            var @event = Context.ScriptInterface.GetEvent(eventTrigger);
            if (@event == null)
            {
                await Context.Reply(nameof(NewScript), "Invalid event name.");
                return;
            }

            _currentScript = new ScriptBuilder(name, null, @event);
            await ReplyAsync($"Now building script: `{name}`, on event: `{eventTrigger}`.");
        }

        [Command("if")]
        public async Task If(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            _currentScript.If(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [Command("and")]
        public async Task And(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            _currentScript.And(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [Command("or")]
        public async Task Or(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            _currentScript.Or(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [Command("then")]
        public async Task Then()
        {
            if (await CheckScriptInProgress())
                _currentScript.Then();
        }

        [Command("else")]
        public async Task Else()
        {
            if (await CheckScriptInProgress())
                _currentScript.Else();
        }

        [Command("elif")]
        public async Task ElseIf(string @class, string func, params string[] @params)
        {
            if (!await CheckScriptInProgress())
                return;
            _currentScript.Else();
            await If(@class, func, @params);
        }

        [Command("end")]
        public async Task End()
        {
            if (await CheckScriptInProgress())
                _currentScript.End();
        }

        [Command("action")]
        public async Task Action(string @class, string func, params string[] @params)
        {
            if (await CheckScriptInProgress())
                _currentScript.Action(ResolveCall(@class, func, @params));
        }

        [Command("done")]
        public async Task Done(string option = null)
        {
            if (!await CheckScriptInProgress())
                return;
            Context.ScriptManager.AddScript(_currentScript.Name,
                                            _currentScript.Description,
                                            Context.Guild.Id,
                                            Context.User.Id.ToString(),
                                            _currentScript.SourceEvent.Name,
                                            _currentScript.Finish());

            if (option != null && option.ToLower() == "enable")
                Context.ScriptManager.ActivateScript(_currentScript.Name);

            await ReplyAsync($"Script '{_currentScript.Name}' created.");
            _currentScript = null;
        }

        private async Task<bool> CheckScriptInProgress()
        {
            if (_currentScript != null)
                return true;
            await ReplyAsync("No script being built!");
            return false;
        }

        private bool CheckThen(string[] @params, bool doThen)
        {
            if (@params.Length == 0 || @params[@params.Length - 1].ToLower() != "then")
                return false;

            if (doThen)
                _currentScript.Then();
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

            // TODO: SUPPORT CALLING FUNCTIONS HERE
            var actualParams = new List<IExpression>();
            int offset = CheckThen(@params, false) ? 1 : 0;
            for (int i = 0; i < @params.Length - offset; i++)
                actualParams.Add(new ConstantExpression { Value = @params[i] });

            return new ScriptBuilder.CallInfo
            {
                Ref = isRef ? @ref[0] : @class,
                ParamName = !isRef ? @class : null,
                ClassName = isRef ? @class.Substring(0, @class.IndexOf('[')) : null,
                FuncName = func,
                FuncParams = actualParams.ToArray()
            };
        }
    }
}