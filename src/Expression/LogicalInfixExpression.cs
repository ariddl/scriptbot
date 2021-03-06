using System;
using System.Text;
using System.Diagnostics;

namespace DiscordScriptBot.Expression
{
    public class LogicalInfixExpression : IEvaluable
    {
        private IEvaluable _condition;
        private IOperator _connector;
        private LogicalInfixExpression _next;

        public LogicalInfixExpression(IEvaluable cond)
        {
            _condition = cond;
            _connector = null;
            _next = null;
        }

        public bool Evaluate() => _next != null ? 
            _connector.Evaluate(_condition, _next) : _condition.Evaluate();

        public LogicalInfixExpression And(IEvaluable e) => Connect(e, AndOp.Instance);
        public LogicalInfixExpression Or(IEvaluable e) => Connect(e, OrOp.Instance);
        public LogicalInfixExpression Xor(IEvaluable e) => Connect(e, XorOp.Instance);

        private LogicalInfixExpression Connect(IEvaluable next, IOperator connector)
        {
            Debug.Assert(_next == null, "Expression already has a connecting expression!");
            var expr = new LogicalInfixExpression(next);
            _next = expr;
            _connector = connector;
            return expr;
        }

        public override string ToString()
        {
            var b = new StringBuilder();
            GetString(b);
            return b.ToString();
        }

        private void GetString(StringBuilder b)
        {
            if (_next == null)
            {
                b.Append(_condition.Evaluate());
                return;
            }

            b.Append($"({_condition.Evaluate()} {_connector.Text} ");
            _next.GetString(b);
            b.Append(")");
        }
    }
}
