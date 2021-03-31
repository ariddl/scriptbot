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
            => IfFalse == null ? Expression.IfThen(TestExpr(context), TrueExpr(context)) :
               Expression.IfThenElse(TestExpr(context), TrueExpr(context), FalseExpr(context));

        private Expression TestExpr(BuildContext context) => Test.Build(context);
        private Expression TrueExpr(BuildContext context)
            => IfTrue != null ? IfTrue.Build(context) : Expression.Empty();
        private Expression FalseExpr(BuildContext context) => IfFalse.Build(context);
    }
}
