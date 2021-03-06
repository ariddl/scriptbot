using DiscordScriptBot.Expression;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DiscordScriptBot.Script
{
    public class ScriptBuilder
    {
        public class BuildException : Exception
        {
            public BuildException(string function, string message)
                : base($"ScriptBuilder.{function}() {message}")
            {
            }
        }

        private string _name;
        private string _description;
        private Stack<IScriptNode> _nodeStack;

        public ScriptBuilder(string name, string description)
        {
            _name = name;
            _description = description;
            _nodeStack = new Stack<IScriptNode>();
            Push<Script.ActionsNode>();
        }

        public void Action(Action a) => Peek<Script.ActionsNode>().Actions.Add(IScriptNode.Wrap(a));

        public void Condition(IEvaluable cond)
        {
            var actionsNode = Peek<Script.ActionsNode>();
            var conditionNode = Push<Script.ConditionNode>();
            conditionNode.Condition = cond;
            actionsNode.Actions.Add(conditionNode);
        }

        public void ConditionPassed() => Peek<Script.ConditionNode>().Passed = Push<Script.ActionsNode>();
        public void ConditionFailed() => Peek<Script.ConditionNode>().Failed = Push<Script.ActionsNode>();

        private T Push<T>() where T : IScriptNode, new()
        {
            T n = new T();
            _nodeStack.Push(n);
            return n;
        }

        public void Pop()
        {
            Debug.Assert(_nodeStack.Count > 1, "ScriptBuilder.Pop() cannot pop root node!");
            _nodeStack.Pop();
        }

        private T Peek<T>() where T : IScriptNode
        {
            string typeName = typeof(T).Name;
            Assert(_nodeStack.Count > 0, $"Peek<{typeName}>", "stack is empty! (already built?)");
            Assert(_nodeStack.Peek() is T, $"Peek<{typeName}>", $"top node not a(n) {typeName}!");
            return (T)_nodeStack.Peek();
        }

        public Script Build()
        {
            Assert(_nodeStack.Count == 1, "Build", "stack is not at root!");
            var script = new Script(_name, _description, _nodeStack.Peek());
            _nodeStack.Clear();
            return script;
        }

        private static void Assert(bool condition, string function, string message)
        {
            if (!condition)
                throw new BuildException(function, message);
        }
    }
}
