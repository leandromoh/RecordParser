using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Generic.GenericRecordParser;

public delegate T FuncSpanIntT<T>(ReadOnlySpan<T> span, int index);
public delegate T FuncSpanT<T>(ReadOnlySpan<char> text);
public delegate T FuncSpanArrayT<T>(ReadOnlySpan<char> line, ReadOnlySpan<(int start, int length)> config);
public delegate T FuncTSpanArrayT<T>(T instance, ReadOnlySpan<char> line, ReadOnlySpan<(int start, int length)> config);

namespace RecordParser.Generic
{
    internal static class SpanExpressionParser
    {
        public static Expression<FuncSpanArrayT<T>> RecordParserSpan<T>(IEnumerable<MappingReadConfiguration> mappedColumns)
        {
            var funcThatSetProperties = GetFuncThatSetPropertiesSpan<T>(mappedColumns);
            var getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop));

            var instanceParameter = funcThatSetProperties.Parameters[0];
            var valueParameter = funcThatSetProperties.Parameters[1];
            var configParameter = funcThatSetProperties.Parameters[2];

            var instanceVariable = Expression.Variable(typeof(T), "inst");
            var assign = Expression.Assign(instanceVariable, getNewInstance.Body);
            var body = new ParameterReplacerVisitor(instanceVariable, instanceParameter).Visit(funcThatSetProperties.Body);
            var block = body as BlockExpression;

            Expression set = Expression.Block(
                typeof(T),
                variables: block != null ? block.Variables.Prepend(instanceVariable) : new[] { instanceVariable },
                expressions: block != null ? block.Expressions.Prepend(assign) : new[] { assign, body });

            var result = Expression.Lambda<FuncSpanArrayT<T>>(set, valueParameter, configParameter);

            return result;
        }

        private static Expression<FuncTSpanArrayT<T>> GetFuncThatSetPropertiesSpan<T>(IEnumerable<MappingReadConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression span = Expression.Variable(typeof(ReadOnlySpan<char>), "span");
            ParameterExpression configParameter = Expression.Variable(typeof(ReadOnlySpan<(int start, int length)>), "config");

            var blockExpr = MountSetProperties(objectParameter, mappedColumns, (i, mapConfig) =>
            {
                (Expression startIndex, Expression length) = (null, null);

                if (mapConfig.length.HasValue)
                {
                    (startIndex, length) = (Expression.Constant(mapConfig.start), Expression.Constant(mapConfig.length.Value));
                }
                else
                {
                    var arrayIndex = ReadOnlySpanIndex<(int, int)>(configParameter, Expression.Constant(i));
                    (startIndex, length) = (Expression.Field(arrayIndex, "Item1"), Expression.Field(arrayIndex, "Item2"));
                }

                var textValue =
                    Expression.Call(span, nameof(ReadOnlySpan<char>.Slice), Type.EmptyTypes, startIndex, length);

                var shouldTrim = mapConfig.prop.Type == typeof(string)
                              || mapConfig.prop.Type == typeof(char)
                              || (mapConfig.prop.Type == typeof(DateTime) && mapConfig.fmask != null);

                return shouldTrim
                    ? Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, textValue)
                    : textValue;
            },
            GetIsWhiteSpaceExpression);

            return Expression.Lambda<FuncTSpanArrayT<T>>(blockExpr, new[] { objectParameter, span, configParameter });
        }

        private static Expression GetIsWhiteSpaceExpression(Expression valueText)
        {
            return Expression.Call(typeof(MemoryExtensions),
                nameof(MemoryExtensions.IsWhiteSpace),
                Type.EmptyTypes, valueText);
        }

        private static Expression ReadOnlySpanIndex<T>(params Expression[] args)
        {
            Debug.Assert(args.Length == 2);

            return GetExpressionFunc((FuncSpanIntT<T>)GetItem, args);
        }

        private static T GetItem<T>(this ReadOnlySpan<T> span, int i) => span[i];
    }
}
