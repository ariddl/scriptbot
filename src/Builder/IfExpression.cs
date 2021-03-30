using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class IfExpression : IExpression
    {
        public ExprType Type => ExprType.If;
        public IExpression Test { get; set; }
        public IExpression IfTrue { get; set; }
        public IExpression IfFalse { get; set; }

        public Expression Build(BuildContext context)
            => IfFalse == null ? Expression.IfThen(Test.Build(), IfTrue.Build())
                : Expression.IfThenElse(Test.Build(), IfTrue.Build(), IfFalse.Build());
    }
}
