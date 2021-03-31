using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class IfExpression : IExpression
    {
        public ExprType Type => ExprType.If;
        public IExpression Test { get; set; }
        public IExpression IfTrue { get; set; }
        public IExpression IfFalse { get; set; }

        public Expression Build(BuildContext ctx)
            => IfFalse == null ? Expression.IfThen(Test.Build(ctx), IfTrue.Build(ctx))
                : Expression.IfThenElse(Test.Build(ctx), IfTrue.Build(ctx), IfFalse.Build(ctx));
    }
}
