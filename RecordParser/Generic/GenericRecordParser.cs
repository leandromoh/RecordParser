﻿using RecordParser.Parsers;
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
            IEnumerable<MappingReadConfiguration> mappedColumns,
            Func<int, MappingReadConfiguration, Expression> getTextValue,
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

        public static Func<Expression, Expression, Expression, Expression> WrapInLambdaExpression<T>(this FuncSpanTIntBool<T> convert)
        {
            if (convert == null)
                return null;

            return (span, inst, offset) =>
            {
                var result = Expression.Variable(typeof((bool, int)), "temp");

                return Expression.Block(variables: new[] { result }, 
                    Expression.Assign(result, GetExpressionFunc(convert, span, inst)),
                    Expression.Assign(offset, Expression.PropertyOrField(result, "Item2")),
                    Expression.Not(Expression.PropertyOrField(result, "Item1")));
            };
        }
    }
}