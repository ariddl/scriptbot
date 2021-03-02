namespace DiscordScriptBot.Expression
{
    public interface IOperator
    {
        string Text { get; }
        bool Evaluate(IEvaluable a, IEvaluable b);
    }

    public class And : IOperator
    {
        public string Text => "&&";
        public bool Evaluate(IEvaluable a, IEvaluable b) => a.Evaluate() && b.Evaluate();
    }

    public class Or : IOperator
    {
        public string Text => "||";
        public bool Evaluate(IEvaluable a, IEvaluable b) => a.Evaluate() || b.Evaluate();
    }

    public class Xor : IOperator
    {
        public string Text => "^";
        public bool Evaluate(IEvaluable a, IEvaluable b)
        {
            bool first = a.Evaluate(), second = b.Evaluate();
            return first != second && (first || second);
        }
    }
}