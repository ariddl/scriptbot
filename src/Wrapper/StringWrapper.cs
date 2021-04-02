using DiscordScriptBot.Builder;

namespace DiscordScriptBot.Wrapper
{
    [WrapperDecl("string", "Generic string")]
    public class StringWrapper : IWrapper
    {
        private string _value;

        public void Init(object value) => _value = (string)value;
        public bool InitRef(BuildContext context, CallExpression.ClassRef @ref) => false;
        public LiteralType LiteralsAllowed => LiteralType.None;

        [WrapperDecl("lower", "Lowercase the string.")]
        public void Lower() => _value = _value.ToLower();

        [WrapperDecl("upper", "Uppercase the string.")]
        public void Upper() => _value = _value.ToUpper();

        [WrapperDecl("empty", "If the string is empty.")]
        public bool Empty() => _value.Length == 0;

        [WrapperDecl("contains", "If the string contains the specified substring.")]
        public bool Contains(string value) => _value.Contains(value);

        [WrapperDecl("startsWith", "If the string starts with the specified substring.")]
        public bool StartsWith(string value) => _value.StartsWith(value);

        [WrapperDecl("endsWith", "If the string ends with the specified substring.")]
        public bool EndsWith(string value) => _value.EndsWith(value);

        [WrapperDecl("equals", "If the string matches the specified string.")]
        public bool Equals(string value) => _value.Equals(value);

        [WrapperDecl("value", "The string value.")]
        public string Value() => _value;

        public override string ToString() => _value;
    }
}
