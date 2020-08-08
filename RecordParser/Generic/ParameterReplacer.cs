using System.Linq.Expressions;

namespace RecordParser.Generic
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/how-to-modify-expression-trees
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly Expression _newParameter;
        private readonly Expression _oldParameter;

        internal ParameterReplacer(Expression newParameter) : this(newParameter, null) { }


        internal ParameterReplacer(Expression newParameter, Expression oldParameter)
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
