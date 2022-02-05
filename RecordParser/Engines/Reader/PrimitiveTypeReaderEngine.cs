using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Engines.ExpressionHelper;

namespace RecordParser.Engines.Reader
{
    internal static class PrimitiveTypeReaderEngine
    {
        public static readonly IReadOnlyDictionary<(Type from, Type to), Func<Type, Expression, Expression>> dic;

        static PrimitiveTypeReaderEngine()
        {
            var mapping = new Dictionary<(Type from, Type to), Func<Type, Expression, Expression>>();

            mapping.AddMapForReadOnlySpan(span => new string(span));
            mapping.AddMapForReadOnlySpan(span => ToChar(span));

            mapping.AddMapForReadOnlySpan(span => byte.Parse(span, NumberStyles.Integer, null));
            mapping.AddMapForReadOnlySpan(span => sbyte.Parse(span, NumberStyles.Integer, null));

            mapping.AddMapForReadOnlySpan(span => double.Parse(span, NumberStyles.AllowThousands | NumberStyles.Float, null));
            mapping.AddMapForReadOnlySpan(span => float.Parse(span, NumberStyles.AllowThousands | NumberStyles.Float, null));

            mapping.AddMapForReadOnlySpan(span => int.Parse(span, NumberStyles.Integer, null));
            mapping.AddMapForReadOnlySpan(span => uint.Parse(span, NumberStyles.Integer, null));

            mapping.AddMapForReadOnlySpan(span => long.Parse(span, NumberStyles.Integer, null));
            mapping.AddMapForReadOnlySpan(span => ulong.Parse(span, NumberStyles.Integer, null));

            mapping.AddMapForReadOnlySpan(span => short.Parse(span, NumberStyles.Integer, null));
            mapping.AddMapForReadOnlySpan(span => ushort.Parse(span, NumberStyles.Integer, null));

            mapping.AddMapForReadOnlySpan(span => Guid.Parse(span));
            mapping.AddMapForReadOnlySpan(span => DateTime.Parse(span, null, DateTimeStyles.AllowWhiteSpaces));
            mapping.AddMapForReadOnlySpan(span => TimeSpan.Parse(span, null));

            mapping.AddMapForReadOnlySpan(span => bool.Parse(span));
            mapping.AddMapForReadOnlySpan(span => decimal.Parse(span, NumberStyles.Number, null));

            mapping[(typeof(ReadOnlySpan<char>), typeof(Enum))] = GetEnumFromSpanParseExpression;

            dic = mapping;
        }

        private static char ToChar(ReadOnlySpan<char> span) => span[0];

        private static void AddMapForReadOnlySpan<T>(
            this IDictionary<(Type, Type), Func<Type, Expression, Expression>> dic,
            Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            dic.Add((typeof(ReadOnlySpan<char>), typeof(T)), GetExpressionExpChar(ex));
        }

        private static Func<Type, Expression, Expression> GetExpressionExpChar<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            var intTao = new ReadOnlySpanVisitor().Modify(ex);

            return (Type _, Expression valueText) => new ParameterReplacerVisitor(valueText).Visit(intTao.Body);
        }

        private static Expression GetEnumFromSpanParseExpression(Type type, Expression span)
        {
            Debug.Assert(type.IsEnum);
            Debug.Assert(span.Type == typeof(ReadOnlySpan<char>));

            var under = Enum.GetUnderlyingType(type);
            var trim = Expression.Variable(typeof(ReadOnlySpan<char>), "trim");
            var number = Expression.Variable(under, "number");

            var body = Enum.GetValues(type)
                .Cast<object>()
                .Select(color =>
                {
                    var enumText = color.ToString();

                    var compareTo = Expression.Call(typeof(MemoryExtensions), "CompareTo", Type.EmptyTypes,
                        StringAsSpan(Expression.Constant(enumText)),
                        trim,
                        Expression.Constant(StringComparison.OrdinalIgnoreCase));

                    var textEqual = Expression.Equal(compareTo, Expression.Constant(0));

                    var lengthEqual = Expression.Equal(
                        Expression.PropertyOrField(trim, "Length"),
                        Expression.Constant(enumText.Length));

                    return (value: color, condition: Expression.AndAlso(lengthEqual, textEqual));
                })
                .Reverse()
                .Aggregate((Expression)Expression.Condition(
                        test: Expression.Call(under, "TryParse", Type.EmptyTypes, trim, number),
                        ifTrue: Expression.Convert(number, type),
                        ifFalse: Expression.Call(typeof(Enum), "Parse", new[] { type },
#if NET6_0_OR_GREATER
                            span, 
#else
                            SpanAsString(trim),
#endif
                            Expression.Constant(true))),
                        (acc, item) =>
                            Expression.Condition(
                                item.condition,
                                Expression.Constant(item.value, type),
                                acc));

            var blockExpr = Expression.Block(
                                variables: new[] { trim, number },
                                expressions: new[]
                                {
                                    Expression.Assign(trim, Trim(span)),
                                    body
                                });

            return blockExpr;
        }
    }
}
