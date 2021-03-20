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
        public static readonly IReadOnlyDictionary<(Type, Type), Func<Type, Expression, Expression>> dic;

        static PrimitiveTypeReaderEngine()
        {
            var mapping = new Dictionary<(Type, Type), Func<Type, Expression, Expression>>();

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

        public static Expression GetParseExpression(Type type, Expression valueText)
        {
            return Expression.Call(
                typeof(Convert), nameof(Convert.ChangeType), Type.EmptyTypes,
                arguments: new[]
                {
                    valueText,
                    Expression.Constant(type, typeof(Type)),
                    Expression.Constant(CultureInfo.InvariantCulture)
                });
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
                    var text = Expression.Call(typeof(MemoryExtensions), "CompareTo", Type.EmptyTypes,
                        StringAsSpan(Expression.Constant(color.ToString())),
                        trim,
                        Expression.Constant(StringComparison.OrdinalIgnoreCase));

                    var textCompare = Expression.Equal(text, Expression.Constant(0));

                    var length = Expression.Equal(
                        Expression.PropertyOrField(trim, "Length"),
                        Expression.Constant(color.ToString().Length));

                    return (value: color, condition: Expression.AndAlso(length, textCompare));
                })
                .Reverse()
                .Aggregate((Expression)Expression.Condition(
                        Expression.Call(under, "TryParse", Type.EmptyTypes, trim, number),
                        Expression.Convert(number, type),
                        Expression.Throw(
                            Expression.New(
                                typeof(ArgumentException).GetConstructor(new[] { typeof(string) }),
                                Expression.Call(typeof(string), "Format", Type.EmptyTypes,
                                    Expression.Constant($"value {{0}} not present in enum {type.Name}"),
                                    Expression.Call(trim, "ToString", Type.EmptyTypes)

                            )), type)),

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
