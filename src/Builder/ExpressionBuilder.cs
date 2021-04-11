using System;
using System.Collections.Generic;

namespace DiscordScriptBot.Builder
{
    /// <summary>
    /// Actions should be independent of elements.
    /// 
    /// /if condition
    /// /action user.kick
    /// /action send to channel "lol" kicked
    /// /action channel@welcome
    /// /else
    /// /if othercondition
    /// /else
    /// /action send to channel "lol"
    /// /end
    /// /end
    /// </summary>
    public class ExpressionBuilder
    {
        private class BuilderException : Exception
        {
            public BuilderException(string func, string msg)
                : base($"ExpressionBuilder.{func}(): {msg}")
            {
            }
        }

        public string Name { get; set; }
        public string Description { get; set; }

        private Stack<IExpression> _exprStack;

        public ExpressionBuilder()
        {
            _exprStack = new Stack<IExpression>();
        }

        public void If() => Push<IfExpression>(); 
        public void Else() => Peek<IfExpression>().IfFalse = Push<BlockExpression>();

        public void Call(string @class, string func, string refType, string @ref, params IExpression[] @params)
            => AppendExpr(new CallExpression
            {
                ClassName = @class,
                FuncName = func,
                Ref = new CallExpression.ClassRef { RefType = refType, Value = @ref },
                Parameters = @params
            });

        private T Push<T>() where T : IExpression, new()
        {
            T t = new T();
            _exprStack.Push(t);
            return t;
        }

        private T Peek<T>() where T : IExpression
        {
            string typeName = typeof(T).Name;
            Assert(_exprStack.Count > 0, $"Peek<{typeName}>", "stack empty!");
            Assert(_exprStack.Peek() is T, $"Peek<{typeName}>", "not at top of stack!");
            return (T)_exprStack.Peek();
        }

        private T Pop<T>() where T : IExpression
        {
            T expr = Peek<T>();
            _exprStack.Pop();
            return expr;
        }

        private void AppendExpr(IExpression expr)
            => Peek<BlockExpression>().Expressions.Add(expr);

        public Func<bool> Compile()
        {
            Assert(_exprStack.Count > 0, "Compile", "stack empty!");
            while (_exprStack.Count > 1)
                _exprStack.Pop();

            var root = Pop<BlockExpression>();
            return null; // TODO - Move/use from ScriptExecutor
            //return root.Build(null);
        }

        private static void Assert(bool cond, string func, string msg)
        {
            if (!cond)
                throw new BuilderException(func, msg);
        }
    }
}
