using System;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public class ReadOnlySpanVisitor : ExpressionVisitor
    {
        public readonly ParameterExpression span = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");

        public delegate T FuncConvert<T>(ReadOnlySpan<char> text);

        public Expression<FuncConvert<T>> Modify<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            if (ex is null) return null;

            var body = Visit(ex.Body);

            var lamb = Expression.Lambda<FuncConvert<T>>(body, span);

            return lamb;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return span;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert
             && node.Type == span.Type
             && node.Operand.Type == typeof(ReadOnlySpanChar))
                return span;

            return base.VisitUnary(node);
        }
    }
}
