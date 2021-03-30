using System.Collections.Generic;
using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class BlockExpression : IExpression
    {
        public ExprType Type => ExprType.Block;
        public List<IExpression> Expressions { get; set; }
            = new List<IExpression>();

        public Expression Build(BuildContext context)
            => Expression.Block(
                Expression.Block(Expressions.ConvertAll(e => e.Build(context))),
                Expression.Constant(true) // return value
               );
    }
}
