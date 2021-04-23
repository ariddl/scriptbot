using DiscordScriptBot.Builder;
using System;
using System.Collections.Generic;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Script
{
    public class ScriptBuilder
    {
        private class BuilderException : Exception
        {
            public BuilderException(string func, string msg)
                : base($"ScriptBuilder.{func}(): {msg}")
            {
            }
        }

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
            Push<BlockExpression>();
        }

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

        private void AppendToBlock(IExpression e) => Peek<BlockExpression>().Expressions.Add(e);
        private void AppendToBlock<T>() where T : IExpression, new()
            => Peek<BlockExpression>().Expressions.Add(Push<T>());

        public void If(CallInfo info)
        {
            AppendToBlock<IfExpression>();
            Peek<IfExpression>().Test = ResolveCall(info);
        }

        public void Else()
        {
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
            while (_exprStack.Count > 1)
                _exprStack.Pop();
            return Peek<BlockExpression>();
        }

        private CallExpression ResolveCall(CallInfo info)
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
            var @if = Peek<IfExpression>();
            e.Left = @if.Test;
            e.Right = cond;
            @if.Test = e;
        }

        private static void Assert(bool cond, string func, string msg)
        {
            if (!cond)
                throw new BuilderException(func, msg);
        }
    }
}
