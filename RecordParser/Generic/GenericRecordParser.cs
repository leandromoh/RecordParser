using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    internal static class GenericRecordParser
    {
        public static BlockExpression MountSetProperties(
            ParameterExpression objectParameter,
            IEnumerable<MappingConfiguration> mappedColumns,
            Func<int, MappingConfiguration, Expression> getTextValue,
            Func<Expression, Expression> getIsNullOrWhiteSpace)
        {
            var replacer = new ParameterReplacerVisitor(objectParameter);
            var assignsExpressions = new List<Expression>();
            var i = -1;

            foreach (var x in mappedColumns)
            {
                i++;

                if (x.prop is null)
                    continue;

                Expression textValue = getTextValue(i, x);

                var propertyType = x.prop.Type;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
                var isPropertyNullable = nullableUnderlyingType != null;
                var propertyUnderlyingType = nullableUnderlyingType ?? propertyType;

                Expression valueToBeSetExpression = GetValueToBeSetExpression(
                                                        propertyUnderlyingType,
                                                        textValue,
                                                        x.fmask);

                if (valueToBeSetExpression.Type != propertyType)
                {
                    valueToBeSetExpression = Expression.Convert(valueToBeSetExpression, propertyType);
                }

                if (isPropertyNullable)
                {
                    valueToBeSetExpression = Expression.Condition(
                        test: getIsNullOrWhiteSpace(textValue),
                        ifTrue: Expression.Constant(null, propertyType),
                        ifFalse: valueToBeSetExpression);
                }

                var assign = Expression.Assign(replacer.Visit(x.prop), valueToBeSetExpression);

                assignsExpressions.Add(assign);
            }

            assignsExpressions.Add(objectParameter);

            var blockExpr = Expression.Block(assignsExpressions);

            return blockExpr;
        }

        public static Expression GetValueToBeSetExpression(Type propertyType, Expression valueText, Expression func)
        {
            if (func != null)
                if (func is LambdaExpression lamb)
                    return new ParameterReplacerVisitor(valueText).Visit(lamb.Body);
                else
                    return Expression.Invoke(func, valueText);

            var targetType = propertyType.IsEnum ? typeof(Enum) : propertyType;

            if (dic.TryGetValue((valueText.Type, targetType), out var expF))
                return expF(propertyType, valueText);

            return GetParseExpression(propertyType, valueText);
        }

        private static readonly IReadOnlyDictionary<(Type, Type), Func<Type, Expression, Expression>> dic;

        static GenericRecordParser()
        {
            var mapping = new Dictionary<(Type, Type), Func<Type, Expression, Expression>>();

            mapping.AddMapForReadOnlySpan(span => new string(span));
            mapping.AddMapForReadOnlySpan(span => ToChar(span));

            mapping.AddMapForReadOnlySpan(span => byte.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));
            mapping.AddMapForReadOnlySpan(span => sbyte.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));

            mapping.AddMapForReadOnlySpan(span => double.Parse(span, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture));
            mapping.AddMapForReadOnlySpan(span => float.Parse(span, NumberStyles.AllowThousands | NumberStyles.Float, CultureInfo.InvariantCulture));

            mapping.AddMapForReadOnlySpan(span => int.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));
            mapping.AddMapForReadOnlySpan(span => uint.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));

            mapping.AddMapForReadOnlySpan(span => long.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));
            mapping.AddMapForReadOnlySpan(span => ulong.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));

            mapping.AddMapForReadOnlySpan(span => short.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));
            mapping.AddMapForReadOnlySpan(span => ushort.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture));

            mapping.AddMapForReadOnlySpan(span => Guid.Parse(span));
            mapping.AddMapForReadOnlySpan(span => DateTime.Parse(span, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces));

            mapping.AddMapForReadOnlySpan(span => bool.Parse(span));
            mapping.AddMapForReadOnlySpan(span => decimal.Parse(span, NumberStyles.Number, CultureInfo.InvariantCulture));

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

        private static Expression GetParseExpression(Type type, Expression valueText)
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

        public static Expression StringAsSpan(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "AsSpan", Type.EmptyTypes, str);

        public static Expression Trim(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, str);

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

                    return (value: color, condition: textCompare);
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

        private static Func<Type, Expression, Expression> GetExpressionExpChar<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            var intTao = new ReadOnlySpanVisitor().Modify(ex);

            return (Type _, Expression valueText) => new ParameterReplacerVisitor(valueText).Visit(intTao.Body);
        }

        public static Expression GetExpressionFunc(Delegate f, params Expression[] args)
        {
            return Expression.Call(f.Target is null ? null : Expression.Constant(f.Target), f.Method, args);
        }

        public static Expression<FuncSpanT<T>> WrapInLambdaExpression<T>(this FuncSpanT<T> convert)
        {
            var arg = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");
            var call = GetExpressionFunc(convert, arg);
            var lambda = Expression.Lambda<FuncSpanT<T>>(call, arg);

            return lambda;
        }

        public static IEnumerable<MappingConfiguration> Merge(
            IEnumerable<MappingConfiguration> list,
            IReadOnlyDictionary<Type, Expression> dic)
        {
            var result = dic.Any() != true
                    ? list
                    : list.Select(i =>
                      {
                          if (i.fmask != null || !dic.TryGetValue(i.type, out var fmask))
                              return i;

                          return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
                      });

            result = result
                .OrderBy(x => x.start)
                .ToArray();

            return result;
        }
    }
}