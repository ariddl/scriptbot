namespace DiscordScriptBot.Element
{
    public class StringElement : IElement
    {
        public string ElementName => "string";
        private string _value;

        public StringElement(string value)
        {
            _value = value;
        }

        [ElementInterface("lower", "Lowercase the string.")]
        public void Lower() => _value = _value.ToLower();

        [ElementInterface("upper", "Uppercase the string.")]
        public void Upper() => _value = _value.ToUpper();

        [ElementInterface("empty", "If the string is empty.")]
        public bool Empty() => _value.Length == 0;

        [ElementInterface("contains", "If the string contains the specified substring.")]
        public bool Contains(string value) => _value.Contains(value);

        [ElementInterface("startsWith", "If the string contains the specified substring.")]
        public bool StartsWith(string value) => _value.StartsWith(value);

        [ElementInterface("endsWith", "If the string contains the specified substring.")]
        public bool EndsWith(string value) => _value.EndsWith(value);

        [ElementInterface("equals", "If the string matches the specified string.")]
        public bool Equals(string value) => _value.Equals(value);
    }
}
