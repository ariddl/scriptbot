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
            // Find the wrapper info for the class. This will contain
            // all the actions/conditionals/properties we can use.
            IWrapperInfo info = context.Interface.GetWrapper(ClassName);
            if (info == null)
                return context.Error($"CallExpression references unknown class: {ClassName}");

            // Determine the way in which we should locate this class instance
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

            string funcName = FuncName;
            if (funcName.Contains("."))
            {
                // funcName is something like text.lower, i.e., a member of a member
                string[] propertyNames = funcName.Split('.');
                for (int i = 0; i < propertyNames.Length - 1; ++i)
                {
                    // The tokens leading up to the function name MUST be properties, since they
                    // return a wrapper that also has functions.
                    string property = propertyNames[i];
                    if (!info.Properties.ContainsKey(property))
                        return context.Error($"CallExpression references unknown property: {property}");
                    
                    // Use reflection to get the instance of the class (reflection is OK for building).
                    var m = info.Properties[property].Info;
                    @class = m.Invoke(@class, null) as IWrapper;

                    // Get the wrapper representing this property
                    info = context.Interface.GetWrapper(m.ReturnType);
                    if (info == null || @class == null)
                        return context.Error($"CallExpression failed to locate property: {property}");
                }

                // The last token is the relative function name
                funcName = propertyNames[propertyNames.Length - 1];
            }

            // Locate the function in the wrapper info
            FunctionInfo func;
            if (info.Actions.ContainsKey(funcName))
            {
                func = info.Actions[funcName];
                if (func.Info.ReturnType == typeof(Task))
                {
                    // To ensure in-order execution of asynchronous calls, we wrap the action call
                    // with a task enqueue function that will enqueue task functions into the
                    // script execution context, which will then run the tasks and await them.
                    var body = Expression.Call(Expression.Constant(@class),
                                               func.Info,
                                               ConvertedParams(context));
                    var taskFunc = Expression.Lambda<Func<Task>>(body).Compile();
                    return Expression.Call(Expression.Constant(context.ExecContext),
                                           typeof(ScriptExecutionContext).GetMethod("EnqueueTask"),
                                           Expression.Constant(taskFunc));
                }
            }
            else if (info.Conditionals.ContainsKey(funcName))
                func = info.Conditionals[funcName];
            else if (info.Properties.ContainsKey(funcName))
                func = info.Properties[funcName];
            else
                return context.Error($"CallExpression references unknown function: {funcName}");

            return Expression.Call(Expression.Constant(@class), func.Info, ConvertedParams(context));
        }

        private static IWrapper InstantiateClass(IWrapperInfo i)
            => (IWrapper)Activator.CreateInstance(i.Type);

        private IEnumerable<Expression> ConvertedParams(BuildContext c)
            => Parameters != null ? Parameters.ToList().ConvertAll(e => e.Build(c)) : new List<Expression>();
    }
}
