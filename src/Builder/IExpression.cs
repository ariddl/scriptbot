using System.Linq.Expressions;

namespace DiscordScriptBot.Builder
{
    public enum ExprType
    {
        Block = 1,
        Constant,
        Call,
        If,
        AndAlso,
        OrElse
    }

    public interface IExpression
    {
        ExprType Type { get; }
        Expression Build(BuildContext context);
    }

    public interface IBinaryExpression : IExpression
    {
        IExpression Left { get; set; }
        IExpression Right { get; set; }
    }
}
