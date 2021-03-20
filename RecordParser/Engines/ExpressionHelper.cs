using System;
using System.Linq.Expressions;

namespace RecordParser.Engines
{
    internal static class ExpressionHelper
    {
        public static Expression Call(Delegate f, params Expression[] args) =>
            Expression.Call(f.Target is null ? null : Expression.Constant(f.Target), f.Method, args);

        public static Expression StringAsSpan(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "AsSpan", Type.EmptyTypes, str);

        public static Expression Trim(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, str);

        public static Expression Slice(Expression span, int start, int length) =>
            Slice(span, start, Expression.Constant(length));

        public static Expression Slice(Expression span, int start, Expression length) =>
            Expression.Call(span, "Slice", Type.EmptyTypes, Expression.Constant(start), length);

        public static Expression IsWhiteSpace(Expression valueText) =>
            Expression.Call(typeof(MemoryExtensions),
                nameof(MemoryExtensions.IsWhiteSpace),
                Type.EmptyTypes, valueText);
    }
}
