using System.Linq.Expressions;

namespace RecordParser.Visitors
{
    internal class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly Expression _newParameter;
        private readonly Expression _oldParameter;

        internal ParameterReplacerVisitor(Expression newParameter) : this(newParameter, null) { }


        internal ParameterReplacerVisitor(Expression newParameter, Expression oldParameter)
        {
            _newParameter = newParameter;
            _oldParameter = oldParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_oldParameter is null || _oldParameter == node)
                return _newParameter;
            
            return node;
        }
    }
}
