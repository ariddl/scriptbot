using System;
using System.Text;

namespace DiscordScriptBot.Expression
{
    public class InfixExpression : IEvaluable
    {
        private IEvaluable _condition;
        private IOperator _connector;
        private InfixExpression _next;

        public InfixExpression(IEvaluable cond)
        {
            _condition = cond;
            _connector = null;
            _next = null;
        }

        public bool Evaluate() => _next != null ? 
            _connector.Evaluate(_condition, _next) : _condition.Evaluate();

        public InfixExpression And(IEvaluable e) => Connect(e, new And()); // todo: operator factory thing
        public InfixExpression Or(IEvaluable e) => Connect(e, new Or());
        public InfixExpression Xor(IEvaluable e) => Connect(e, new Xor());

        private InfixExpression Connect(IEvaluable next, IOperator connector)
        {
            if (_next != null)
                throw new Exception("Expression already has a connecting expression!");
            var expr = new InfixExpression(next);
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
