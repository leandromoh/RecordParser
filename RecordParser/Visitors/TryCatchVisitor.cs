using System;
using System.Linq.Expressions;

namespace RecordParser.Visitors
{
    internal class TryCatchVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _exceptionHandler;
        private readonly ConstantExpression columnIndexExpression;

        public TryCatchVisitor(ParameterExpression exceptionHandler, int columnIndex)
        {
            _exceptionHandler = exceptionHandler;
            columnIndexExpression = Expression.Constant(columnIndex);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType != ExpressionType.Assign)
                return node;

            var ex = Expression.Parameter(typeof(Exception));

            var tryCatch =
                Expression.TryCatch(
                    Expression.Block(typeof(void), node),
                Expression.Catch(
                    ex,
                    Expression.Block(
                        expressions: new[]
                        {
                            Expression.Invoke(_exceptionHandler, ex, columnIndexExpression),
                        })
                ));

            return tryCatch;
        }
    }
}