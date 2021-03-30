using Discord.WebSocket;
using DiscordScriptBot.Script;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DiscordScriptBot.Builder
{
    // Holds references to get event parameters and references to classes by constants (id/string/whatever)
    public class BuildContext
    {
        public DiscordSocketClient Discord { get; set; }
        public SocketGuild Guild { get; set; }
        public ScriptInterface Interface { get; set; }
        public List<string> Errors { get; private set; } = new List<string>();

        public ScriptExecutionContext ExecContext { get; set; }

        public Expression Error(string msg)
        {
            Errors.Add(msg);
            return Expression.Empty();
        }
    }
}
