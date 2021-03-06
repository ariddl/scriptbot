namespace DiscordScriptBot.Expression
{
    public interface IEvaluable
    {
        /// <summary>
        /// Delegate wrapping for lambdas
        /// </summary>
        public delegate bool Delegate();
        private class Wrapped : IEvaluable
        {
            public Delegate Del { get; set; }
            public bool Evaluate() => Del();
        }

        public static IEvaluable Wrap(Delegate del) => new Wrapped { Del = del };

        /// <summary>
        /// IEvaluable interface
        /// </summary>
        bool Evaluate();
    }
}
