using RecordParser.Builders.Reader;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Engines.ExpressionHelper;

internal delegate T FuncSpanIntT<T>(ReadOnlySpan<T> span, int index);
public delegate T FuncSpanT<T>(ReadOnlySpan<char> text);
internal delegate T FuncSpanArrayT<T>(ReadOnlySpan<char> line, ReadOnlySpan<(int start, int length)> config);

namespace RecordParser.Engines.Reader
{
    internal static class ReaderEngine
    {
        public static Expression<FuncSpanArrayT<T>> RecordParserSpan<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var line = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");
            var configParameter = Expression.Parameter(typeof(ReadOnlySpan<(int start, int length)>), "config");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, (i, mapConfig) =>
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

                var textValue = Slice(line, startIndex, length);

                return mapConfig.ShouldTrim
                    ? Trim(textValue)
                    : textValue;
            });

            var getNewInstance = factory != null
                ? Call(factory)
                : CreateInstanceEngine.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop).OfType<MemberExpression>()).Body;

            var assign = Expression.Assign(instanceVariable, getNewInstance);
            var body = Expression.Block(
                typeof(T),
                variables: blockThatSetProperties.Variables.Prepend(instanceVariable),
                expressions: blockThatSetProperties.Expressions.Prepend(assign));

            var result = Expression.Lambda<FuncSpanArrayT<T>>(body, line, configParameter);

            return result;
        }

        private static Expression ReadOnlySpanIndex<T>(params Expression[] args)
        {
            Debug.Assert(args.Length == 2);
            Debug.Assert(args[0].Type == typeof(ReadOnlySpan<T>));
            Debug.Assert(args[1].Type == typeof(int));

            return Call((FuncSpanIntT<T>)GetItem, args);
        }

        private static T GetItem<T>(this ReadOnlySpan<T> span, int i) => span[i];

        private static BlockExpression MountSetProperties(
            ParameterExpression objectParameter,
            IEnumerable<MappingReadConfiguration> mappedColumns,
            Func<int, MappingReadConfiguration, Expression> getTextValue)
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
                        test: IsWhiteSpace(textValue),
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

        private static Expression GetValueToBeSetExpression(Type propertyType, Expression valueText, Delegate func)
        {
            if (func != null)
                return Call(func, valueText);

            var targetType = propertyType.IsEnum ? typeof(Enum) : propertyType;

            if (PrimitiveTypeReaderEngine.dic.TryGetValue((valueText.Type, targetType), out var expF))
                return expF(propertyType, valueText);

            throw new InvalidOperationException($"Type '{propertyType.FullName}' does not have a default parse");
        }
    }
}