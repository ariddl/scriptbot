using DiscordScriptBot.Script;
using DiscordScriptBot.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static DiscordScriptBot.Script.ScriptInterface;

namespace DiscordScriptBot.Builder
{
    public class CallExpression : IExpression
    {
        public class ClassRef
        {
            public const string TypeNone = "none";
            public const string TypeId = "id";
            public const string TypeStr = "str";
            public const string TypeParam = "param";

            public static LiteralType GetLiteralType(string type)
                => type == TypeStr ? LiteralType.String :
                   type == TypeId  ? LiteralType.Int :
                   LiteralType.None;

            public string RefType { get; set; }
            public string Value { get; set; }
        }

        public ExprType Type => ExprType.Call;
        public string ClassName { get; set; }
        public ClassRef Ref { get; set; }
        public string FuncName { get; set; }
        public IExpression[] Parameters { get; set; }

        public Expression Build(BuildContext context)
        {
            IWrapperInfo info = context.Interface.GetWrapper(ClassName);
            if (info == null)
                return context.Error($"CallExpression references unknown class: {ClassName}");

            IWrapper @class = null;
            switch (Ref.RefType)
            {
                case ClassRef.TypeNone:
                    break;
                case ClassRef.TypeId:
                case ClassRef.TypeStr:
                    if (!(@class = InstantiateClass(info)).InitRef(context, Ref))
                        return context.Error($"CallExpression invalid class reference: {Ref.Value}");
                    break;
                case ClassRef.TypeParam:
                    // Our wrappers are initialized in the IEvent.Init functions.
                    @class = (IWrapper)context.ExecContext.GetParam(Ref.Value);
                    if (@class == null)
                        return context.Error($"CallExpression references unknown parameter: {Ref.Value}");
                    break;
            }

            FunctionInfo func;
            if (info.Actions.ContainsKey(FuncName))
            {
                func = info.Actions[FuncName];
                if (func.Info.ReturnType == typeof(Task))
                {
                    // To ensure in-order execution of asynchronous calls, we wrap the action call
                    // with a task enqueue function that will enqueue task functions into the
                    // script execution context, which will then run the tasks and await them.
                    var body = Expression.Call(Expression.Constant(@class),
                                               func.Info,
                                               ConvertedParams);
                    var taskFunc = Expression.Lambda<Func<Task>>(body).Compile();
                    return Expression.Call(Expression.Constant(context.ExecContext),
                                           typeof(ScriptExecutionContext).GetMethod("EnqueueTask"),
                                           Expression.Constant(taskFunc));
                }
            }
            else if (info.Conditionals.ContainsKey(FuncName))
                func = info.Conditionals[FuncName];
            else if (info.Properties.ContainsKey(FuncName))
                func = info.Properties[FuncName];
            else
                return context.Error($"CallExpression references unknown function: {FuncName}");

            return Expression.Call(Expression.Constant(@class), func.Info, ConvertedParams);
        }

        private static IWrapper InstantiateClass(IWrapperInfo i)
            => (IWrapper)Activator.CreateInstance(i.Type);

        private IEnumerable<Expression> ConvertedParams
            => Parameters != null ? Parameters.ToList().ConvertAll(e => e.Build()) : new List<Expression>();
    }
}
