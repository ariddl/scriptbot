using DiscordScriptBot.Builder;
using System;

namespace DiscordScriptBot.Wrapper
{
    public enum LiteralType
    {
        None,
        Int = 1,
        String = 2
    }

    public interface IWrapper
    {
        void Init(object context);
        bool InitRef(BuildContext context, CallExpression.ClassRef @ref);
        LiteralType LiteralsAllowed { get; }
    }

    public class WrapperDecl : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Target { get; set; }

        public WrapperDecl(string name, string desc, string target = null)
        {
            Name = name;
            Description = desc;
            Target = target;
        }
    }
}
