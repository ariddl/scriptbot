using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordScriptBot.Element
{
    public interface IElement
    {
        string ElementName { get; }
    }

    public class ElementInterface : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string[] Arguments { get; }

        public ElementInterface(string name, string description, params string[] args)
        {
            Name = name;
            Description = description;
            Arguments = args;
        }
    }
}
