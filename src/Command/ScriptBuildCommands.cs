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
            public async Task NewScript(string name, string eventTrigger, string description = null)
            {
                // Try to find the event type they want to use.
                var @event = Context.ScriptInterface.GetEvent(eventTrigger);
                if (@event == null)
                {
                    // Event type is invalid. Scripts currently can only exist with defined events.
                    // TODO: Eventless scripts! (E.g., scheduled scripts, etc).
                    await Context.Reply(nameof(NewScript), "Invalid event name.");
                    return;
                }

                // Create a new script builder for the current user (uses CommandManager context).
                // Note that this will overwrite their current ScriptBuilder if they are already
                // building a script (this is fine for now).
                Context.SetContext(new ScriptBuilder(name, description, @event));
                await ReplyAsync($"Now building script: `{name}`, on event: `{eventTrigger}`.");
            }

            [RequireOwner]
            [Command("done")]
            public async Task Done(string option = null)
            {
                // Get the current user's ScriptBuilder context, check if it exists.
                var currentScript = Context.GetContext<ScriptBuilder>();
                if (currentScript == null)
                    return;

                // Add the script to the ScriptManager. This will add the script meta
                // data, but won't immediately enable it. Enable must be specified.
                Context.ScriptManager.AddScript(currentScript.Name,
                                                currentScript.Description,
                                                Context.Guild.Id,
                                                Context.User.Id.ToString(),
                                                currentScript.SourceEvent.Name,
                                                currentScript.Finish());

                // Check the optional argument for the "enable" flag, and if set,
                // this script will immediately be activated and compiled. In the
                // future, we should probably try to compile the script before
                // allowing the build process to complete (TODO).
                if (option != null && option.ToLower() == "enable")
                    Context.ScriptManager.ActivateScript(currentScript.Name);

                // Delete the ScriptBuilder for this user.
                await ReplyAsync($"Script '{currentScript.Name}' created.");
                Context.RemoveContext<ScriptBuilder>();
            }

            [RequireOwner]
            [Command("cancel")]
            public async Task Cancel()
            {
                // Attempt to get the current ScriptBuilder for this user.
                var currentScript = Context.GetContext<ScriptBuilder>();
                if (currentScript == null)
                    return;

                // Remove the ScriptBuilder to cancel and notify the user.
                Context.RemoveContext<ScriptBuilder>();
                await ReplyAsync($"Script building for `{currentScript.Name}` has been canceled!");
            }
        }

        [RequireOwner]
        [Command("if")]
        public async Task If(string @class, string func, params string[] @params)
        {
            // Make sure we're actually building a script, and if not, notify the user.
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().If(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("and")]
        public async Task And(string @class, string func, params string[] @params)
        {
            // Make sure we're actually building a script, and if not, notify the user.
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().And(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("or")]
        public async Task Or(string @class, string func, params string[] @params)
        {
            // Make sure we're actually building a script, and if not, notify the user.
            if (!await CheckScriptInProgress())
                return;
            Context.GetContext<ScriptBuilder>().Or(ResolveCall(@class, func, @params));
            CheckThen(@params, true);
        }

        [RequireOwner]
        [Command("then")]
        public async Task Then()
        {
            // Make sure we're actually building a script, and if not, notify the user.
            // If we are building a script, call Then() to enter the action block body.
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Then();
        }

        [RequireOwner]
        [Command("else")]
        public async Task Else()
        {
            // Make sure we're actually building a script, and if not, notify the user.
            // If we are building a script, call Else() to enter the action block body.
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Else();
        }

        [RequireOwner]
        [Command("elif")]
        public async Task ElseIf(string @class, string func, params string[] @params)
        {
            // Make sure we're actually building a script, and if not, notify the user.
            if (!await CheckScriptInProgress())
                return;

            // Call Else as normal to enter the else action block body, and then
            // immediately run the If command with the parameters provided. This
            // effectively groups these two commands together into one for convenience!
            Context.GetContext<ScriptBuilder>().Else();
            await If(@class, func, @params);
        }

        [RequireOwner]
        [Command("end")]
        public async Task End()
        {
            // Make sure we're actually building a script, and if not, notify the user.
            // If we are building a script, End() will end the current if statement block.
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().End();
        }

        [RequireOwner]
        [Command("action")]
        public async Task Action(string @class, string func, params string[] @params)
        {
            // Make sure we're actually building a script, and if not, notify the user.
            // If we are building a script, Action() will append a function call to
            // the current code block. These are things like Kick (user),
            // Delete (message), etc.
            if (await CheckScriptInProgress())
                Context.GetContext<ScriptBuilder>().Action(ResolveCall(@class, func, @params));
        }

        private async Task<bool> CheckScriptInProgress()
        {
            // Check if we have a ScriptBuilder context for this user.
            if (Context.GetContext<ScriptBuilder>() != null)
                return true;

            // No ScriptBuilder; they are not currently building a script.
            await ReplyAsync("No script being built!");
            return false;
        }

        private bool CheckThen(string[] @params, bool doThen)
        {
            // @params are all the arguments passed to the command, and we
            // only care about the last one. Particularly, we want to check
            // if the last parameter is a "then", at which point we will
            // call Then() on their ScriptBuilder so they don't have to do
            // the /then command manually. This function should be used for
            // any commands that do any kind of logical expressions (/if, /or, /etc).
            if (@params.Length == 0 || @params[@params.Length - 1].ToLower() != "then")
                return false;

            if (doThen)
                Context.GetContext<ScriptBuilder>().Then();
            return true;
        }

        private static string[] Tokenize(string src, string start, string end)
        {
            // This function will split up all strings that are found in between the
            // start and end strings. Ideally we would use a real tokenizer, but for
            // now this is good for prototyping. (TODO: More robust parsing?)
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
            // First, try to find a "reference" to the class that we want to work with.
            // For our purposes we refer to things like channel names and ids as "references".
            // If there is some string like [this] in our @class argument, we will use it.
            var @ref = Tokenize(@class, "[", "]");
            bool isRef = @ref.Length == 1;

            // Convert our string parameters from the command to our expression interface.
            var actualParams = new List<IExpression>();
            int offset = CheckThen(@params, false) ? 1 : 0;
            for (int i = 0; i < @params.Length - offset; i++)
                actualParams.Add(ResolveParam(@params[i]));

            // The ScriptBuilder uses an internal CallInfo struct that it will use to
            // create a corresponding CallExpression with the given parameters. It will
            // use the SourceEvent it has to query the event parameter names that we
            // may need to reference for our calls.
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
            // Strings like ${} are considered to be call expressions, which use the
            // same format as in ResolveCall. If this isn't found, we'll default to
            // a constant expression. TODO: String formatting using this same system!
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