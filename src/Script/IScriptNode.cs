using System;

namespace DiscordScriptBot.Script
{
    /// <summary>
    /// Runnable node for scripts.
    /// </summary>
    public interface IScriptNode
    {
        /// <summary>
        /// Action delegate wrapping
        /// </summary>
        private class Wrapped : IScriptNode
        {
            public Action Action { get; set; }
            public void Run() => Action();
        }
        public static IScriptNode Wrap(Action a) => new Wrapped { Action = a };

        /// <summary>
        /// IScriptNode interface
        /// </summary>
        void Run();
    }
}
