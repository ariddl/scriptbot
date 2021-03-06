using System;

namespace DiscordScriptBot.Expression
{
    public static class Test
    {
        private static void ExprTest(int flags)
        {
            Func<int, IEvaluable> f = flag => IEvaluable.Wrap(() => (flags & flag) != 0);

            var e = new LogicalInfixExpression(f(1));
            e.And(f(2))
             .Or(f(4))
             .And(f(8))
             .Xor(f(16));
            Console.WriteLine($"Expr test: {e} -> {e.Evaluate()}");
        }

        public static void RunTests()
        {
            ExprTest(1 | 4 | 8);
            ExprTest(1 | 4 | 16);
            ExprTest(1 | 2);
            ExprTest(1);
            ExprTest(16);
        }
    }
}
