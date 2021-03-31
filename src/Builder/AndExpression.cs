using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class AndExpression : IExpression
    {
        public ExprType Type => ExprType.AndAlso;
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }

        public Expression Build(BuildContext context)
            => Expression.AndAlso(Left.Build(context), Right.Build(context));
    }
}