using System;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Visitors
{
    internal class SpanReplacerVisitor : ExpressionVisitor
    {
        private readonly static ParameterExpression memory = Expression.Parameter(typeof(ReadOnlyMemory<char>), "memory");
        private readonly static MemberExpression span = Expression.Property(memory, "Span");



        public Expression<Func<ReadOnlyMemory<char>, T>> Modify<T>(Expression<FuncSpanT<T>> ex)
        {
            if (ex is null) return null;

            var body = Visit(ex.Body);

            body = new StaticMethodVisitor().Visit(body);

            var lamb = Expression.Lambda<Func<ReadOnlyMemory<char>, T>>(body, memory);

            return lamb;
        }

    

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == typeof(ReadOnlySpan<char>))
                return span;

            return base.VisitParameter(node);
        }

        class StaticMethodVisitor : ExpressionVisitor
        {
            public static string ToString(ReadOnlySpan<char> span) => span.ToString();
            public static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span) => span.Trim();
            public static ReadOnlySpan<char> Slice1(ReadOnlySpan<char> span, int start) => span.Slice(start);
            public static ReadOnlySpan<char> Slice2(ReadOnlySpan<char> span, int start, int count) => span.Slice(start, count);

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Object?.Type == typeof(ReadOnlySpan<char>))
                {
                    var args = node.Arguments.Prepend(node.Object).ToArray();

                    if (node.Method.Name == "Slice")
                    {
                        Delegate f = node.Arguments.Count == 1 
                            ? StaticMethodVisitor.Slice1
                            : StaticMethodVisitor.Slice2;

                        return Expression.Call(f.Method, args);
                    }

                    if (node.Method.Name == "ToString")
                    {
                        var f = StaticMethodVisitor.ToString;

                        return Expression.Call(f.Method, args);
                    }

                    if (node.Method.Name == "Trim")
                    {
                        var f = StaticMethodVisitor.Trim;

                        return Expression.Call(f.Method, args);
                    }
                }

                return base.VisitMethodCall(node);
            }
        }
    }
}
