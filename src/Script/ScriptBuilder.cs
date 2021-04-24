using DiscordScriptBot.Builder;
using System;
using System.Collections.Generic;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Script
{
    public class ScriptBuilder
    {
        public struct CallInfo
        {
            public string Ref { get; set; }
            public string ClassName { get; set; }
            public string ParamName { get; set; }
            public string FuncName { get; set; }
            public IExpression[] FuncParams { get; set; }
        }

        public static CallInfo CallByRef(string @ref, string func, IExpression[] @params)
            => new CallInfo { Ref = @ref, FuncName = func, FuncParams = @params };
        public static CallInfo CallByParam(string param, string func, IExpression[] @params)
            => new CallInfo { ParamName = param, FuncName = func, FuncParams = @params };

        public string Name { get; }
        public string Description { get; }
        public IWrapperInfo SourceEvent { get; }

        private Stack<IExpression> _exprStack;

        public ScriptBuilder(string name, string description, IWrapperInfo @event)
        {
            Name = name;
            Description = description;
            SourceEvent = @event;
            _exprStack = new Stack<IExpression>();
            
            // The root node must always be a Block for scripts.
            Push<BlockExpression>();
        }

        private T Push<T>() where T : IExpression, new()
        {
            // Instantiate T and push it to the top of the stack.
            T t = new T();
            _exprStack.Push(t);
            return t;
        }

        private T Peek<T>() where T : IExpression
        {
            // Peek the top element while ensuring it is the type we expect
            string typeName = typeof(T).Name;
            Assert(_exprStack.Count > 0, $"Peek<{typeName}>", "stack empty!");
            Assert(_exprStack.Peek() is T, $"Peek<{typeName}>", "not at top of stack!");
            return (T)_exprStack.Peek();
        }

        private T Pop<T>() where T : IExpression
        {
            // Pop the top element and return it as the type we expect.
            T expr = Peek<T>();
            _exprStack.Pop();
            return expr;
        }

        // Functions to append expressions to the end of the current block
        private void AppendToBlock(IExpression e) => Peek<BlockExpression>().Expressions.Add(e);
        private void AppendToBlock<T>() where T : IExpression, new()
            => Peek<BlockExpression>().Expressions.Add(Push<T>());

        public void If(CallInfo info)
        {
            // Append a new IfExpression to our current block. Set the condition.
            AppendToBlock<IfExpression>();
            Peek<IfExpression>().Test = ResolveCall(info);
        }

        public void Else()
        {
            // If we're calling else, we must be an if-block. Pop it.
            // Next peek the IfExpression and push a block for the false condition.
            Pop<BlockExpression>();
            Peek<IfExpression>().IfFalse = Push<BlockExpression>();
        }

        public void And(CallInfo info) => ChainConditional(new AndExpression(), ResolveCall(info));

        public void Or(CallInfo info) => ChainConditional(new OrExpression(), ResolveCall(info));

        public void Then() => Peek<IfExpression>().IfTrue = Push<BlockExpression>();

        public bool End() => Pop<BlockExpression>() != null && Pop<IfExpression>() != null;

        public void Action(CallInfo info) => AppendToBlock(ResolveCall(info));

        public BlockExpression Finish()
        {
            // Pop all nodes up to the root node (default block).
            while (_exprStack.Count > 1)
                _exprStack.Pop();
            return Peek<BlockExpression>();
        }

        public CallExpression ResolveCall(CallInfo info)
        {
            string refType = CallExpression.ClassRef.TypeParam;
            if (info.ClassName == null)
            {
                // Events only have properties (wrappers) which act as parameters.
                // These properties contain all callable functions.
                if (!SourceEvent.Properties.ContainsKey(info.ParamName))
                    return null;
                info.ClassName = SourceEvent.Properties[info.ParamName].Attribute.Name;
            }
            else
                refType = ulong.TryParse(info.Ref, out ulong _)
                    ? CallExpression.ClassRef.TypeId : CallExpression.ClassRef.TypeStr;

            // Build the serializable CallExpression from our info
            return new CallExpression
            {
                ClassName = info.ClassName,
                FuncName = info.FuncName,
                Parameters = info.FuncParams,
                Ref = new CallExpression.ClassRef
                {
                    RefType = refType,
                    Value = info.Ref
                }
            };
        }
        
        private void ChainConditional(IBinaryExpression e, IExpression cond)
        {
            // Replace the current test expression in our if with our new
            // binary expression (e) by moving it to the left condition
            // of our new binary expression, and setting the right to cond.
            var @if = Peek<IfExpression>();
            e.Left = @if.Test;
            e.Right = cond;
            @if.Test = e;
        }

        private static void Assert(bool cond, string func, string msg)
        {
            // Throw an exception if the condition is not correct. Note that
            // the command handler will catch the exception and display it.
            if (!cond)
                throw new Exception($"ScriptBuilder.{func}(): {msg}");
        }
    }
}
