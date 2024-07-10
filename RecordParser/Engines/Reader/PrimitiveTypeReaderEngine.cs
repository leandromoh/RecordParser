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

            mapping.AddMapForReadOnlySpan(span => span.ToString());
            mapping.AddMapForReadOnlySpan(span => ToChar(span));

            mapping.AddMapForReadOnlySpan(span => Parse.Byte(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.SByte(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Double(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.Single(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Int32(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.UInt32(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Int64(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.UInt64(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Int16(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.UInt16(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Guid(span));
            mapping.AddMapForReadOnlySpan(span => Parse.DateTime(span, null));
            mapping.AddMapForReadOnlySpan(span => Parse.TimeSpan(span, null));

            mapping.AddMapForReadOnlySpan(span => Parse.Boolean(span));
            mapping.AddMapForReadOnlySpan(span => Parse.Decimal(span, null));

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
#if NETSTANDARD2_0
                        test: Expression.Call(under, "TryParse", Type.EmptyTypes, SpanAsString(trim), Expression.Constant(NumberStyles.Number), Expression.Constant(null, typeof(IFormatProvider)), number),
#else
                        test: Expression.Call(under, "TryParse", Type.EmptyTypes, trim, number),
#endif

                        ifTrue: Expression.Convert(number, type),

#if NET6_0_OR_GREATER
                        ifFalse: Expression.Call(typeof(Enum), "Parse", [type], span, Expression.Constant(true))),
#elif NETSTANDARD2_1_OR_GREATER
                        ifFalse: Expression.Call(typeof(Enum), "Parse", [type], SpanAsString(trim), Expression.Constant(true))),
#else
                        ifFalse: Expression.Convert(Expression.Call(typeof(Enum), "Parse", Type.EmptyTypes, Expression.Constant(type), SpanAsString(trim), Expression.Constant(true)), type)),
#endif

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
