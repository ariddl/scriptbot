using DiscordScriptBot.Utility;

namespace DiscordScriptBot.Expression
{
    public interface IOperator
    {
        string Text { get; }
        bool Evaluate(IEvaluable a, IEvaluable b);
    }

    public class AndOp : Singleton<AndOp>, IOperator
    {
        public string Text => "&&";
        public bool Evaluate(IEvaluable a, IEvaluable b) => a.Evaluate() && b.Evaluate();
    }

    public class OrOp : Singleton<OrOp>, IOperator
    {
        public string Text => "||";
        public bool Evaluate(IEvaluable a, IEvaluable b) => a.Evaluate() || b.Evaluate();
    }

    public class XorOp : Singleton<XorOp>, IOperator
    {
        public string Text => "^";
        public bool Evaluate(IEvaluable a, IEvaluable b)
        {
            bool first = a.Evaluate(), second = b.Evaluate();
            return first != second && (first || second);
        }
    }
}