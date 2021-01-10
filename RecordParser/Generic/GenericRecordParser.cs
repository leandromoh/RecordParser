using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public static class GenericRecordParser
    {
        public static Expression GetValueToBeSetExpression(Type propertyType, Expression valueText, Expression func)
        {
            if (func != null)
                if (func is LambdaExpression lamb)
                    return new ParameterReplacer(valueText).Visit(lamb.Body);
                else
                    return Expression.Invoke(func, valueText);

            var targetType = propertyType.IsEnum ? typeof(Enum) : propertyType;

            if (dic.TryGetValue((valueText.Type, targetType), out var expF))
                return expF(propertyType, valueText);

            return GetParseExpression(propertyType, valueText);
        }

        private static readonly IDictionary<(Type, Type), Func<Type, Expression, Expression>> dic = new Dictionary<(Type, Type), Func<Type, Expression, Expression>>
        {
            [(typeof(string), typeof(string))] = (_, ex) => ex,
            [(typeof(string), typeof(Enum))] = GetEnumFromStringParseExpression,
            [(typeof(ReadOnlySpan<char>), typeof(Enum))] = GetEnumFromSpanParseExpression,
            [(typeof(ReadOnlySpan<char>), typeof(int))] = GetExpressionExpChar(span => int.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            [(typeof(ReadOnlySpan<char>), typeof(DateTime))] = GetExpressionExpChar(span => DateTime.Parse(span, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces)),
            [(typeof(ReadOnlySpan<char>), typeof(string))] = GetExpressionExpChar(span => new string(span)),
            [(typeof(ReadOnlySpan<char>), typeof(decimal))] = GetExpressionExpChar(span => decimal.Parse(span, NumberStyles.Number, CultureInfo.InvariantCulture))
        };

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

        private static Expression GetExpressionExp<R>(Expression<Func<string, R>> f, Expression valueText)
        {
            return new ParameterReplacer(valueText).Visit(f.Body);
        }

        private static Expression GetEnumFromStringParseExpression(Type type, Expression valueText)
        {
            Debug.Assert(valueText.Type == typeof(string));

            return Expression.Call(
                typeof(Enum), nameof(Enum.Parse), new[] { type },
                arguments: new Expression[]
                {
                        valueText,
                        Expression.Constant(true)
                });
        }

        private static Expression GetEnumFromSpanParseExpression(Type type, Expression valueText)
        {
            Debug.Assert(valueText.Type == typeof(ReadOnlySpan<char>));

            var stringConstructor = typeof(string).GetConstructor(new[] { typeof(ReadOnlySpan<char>) });

            return Expression.Call(
                typeof(Enum), nameof(Enum.Parse), new[] { type },
                arguments: new Expression[]
                {
                    Expression.New(stringConstructor, valueText),
                    Expression.Constant(true)
                });
        }

        private static Func<Type, Expression, Expression> GetExpressionExpChar<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            var intTao = new ReadOnlySpanVisitor().Modify(ex);

            return (Type _, Expression valueText) => new ParameterReplacer(valueText).Visit(intTao.Body);
        }

        public static Expression GetExpressionFunc<R>(Func<string, R> f, Expression valueText)
        {
            return Expression.Call(f.Target is null ? null : Expression.Constant(f.Target), f.Method, valueText);
        }

        public static IEnumerable<MappingConfiguration> Merge(
            IEnumerable<MappingConfiguration> list,
            IReadOnlyDictionary<Type, Expression> dic)
        {
            var result = dic?.Any() != true
                    ? list
                    : list.Select(i =>
                      {
                          if (i.fmask != null || !dic.TryGetValue(i.type, out var fmask))
                              return i;

                          return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask, i.skipWhen);
                      });

            result = result
                .OrderBy(x => x.start)
                .ToArray();

            return result;
        }
    }
}