using System;
using System.Linq.Expressions;

namespace RecordParser.Visitors
{
    internal struct ReadOnlySpanChar
    {
        public static implicit operator ReadOnlySpan<char>(ReadOnlySpanChar _) => default;
    }

    internal class ReadOnlySpanVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression span = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");

        public Expression<FuncSpanT<T>> Modify<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            if (ex is null) return null;

            var body = Visit(ex.Body);

            var lamb = Expression.Lambda<FuncSpanT<T>>(body, span);

            return lamb;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == typeof(ReadOnlySpanChar))
                return span;

            return base.VisitParameter(node);
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
