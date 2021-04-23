using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public class ConstantExpression : IExpression
    {
        public ExprType Type => ExprType.Constant;
        public string Value { get; set; }

        public Expression Build(BuildContext context)
            => ulong.TryParse(Value, out ulong i) ? Expression.Constant(i) : Expression.Constant(Value);
    }
}
