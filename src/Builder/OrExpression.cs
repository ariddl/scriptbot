using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class OrExpression : IBinaryExpression
    {
        public ExprType Type => ExprType.OrElse;
        public IExpression Left { get; set; }
        public IExpression Right { get; set; }

        public Expression Build(BuildContext context)
            => Expression.OrElse(Left.Build(context), Right.Build(context));
    }
}