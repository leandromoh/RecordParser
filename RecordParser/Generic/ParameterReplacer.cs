using System.Linq.Expressions;

namespace RecordParser.Generic
{
    // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/how-to-modify-expression-trees
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly Expression _parameter;

        internal ParameterReplacer(Expression parameter)
        {
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameter;
        }
    }
}
