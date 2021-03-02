namespace DiscordScriptBot.Expression
{
    public interface IEvaluable
    {
        bool Evaluate();
    }

    public delegate bool EvaluableFunc();
    public class WrappedEvaluable : IEvaluable
    {
        private EvaluableFunc _func;

        public WrappedEvaluable(EvaluableFunc func) => _func = func;
        public bool Evaluate() => _func();
    }
}
