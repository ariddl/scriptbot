using DiscordScriptBot.Expression;
using System.Collections.Generic;

namespace DiscordScriptBot.Script
{
    public class Script : IScriptNode
    {
        public class ConditionNode : IScriptNode
        {
            public IEvaluable Condition { get; set; }
            public IScriptNode Passed { get; set; }
            public IScriptNode Failed { get; set; }

            public void Run()
            {
                if (Condition.Evaluate())
                    Passed?.Run();
                else
                    Failed?.Run();
            }
        }

        public class ActionsNode : IScriptNode
        {
            private List<IScriptNode> _actions;
            public List<IScriptNode> Actions => _actions ?? (_actions = new List<IScriptNode>());

            public void Run() => _actions.ForEach(a => a.Run());
        }

        private string _name;
        private string _description;
        private IScriptNode _tree;

        public Script(string name, string description, IScriptNode tree)
        {
            _name = name;
            _description = description;
            _tree = tree;
        }

        public void Run() => _tree.Run();

        public string Name => _name;
        public string Description => _description;
    }
}
