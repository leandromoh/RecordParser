using RecordParser.Builders.Reader;
using RecordParser.Engines.Reader;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Engines.ExpressionHelper;

internal delegate T FuncSpanIntT<T>(ReadOnlySpan<T> span, int index);
public delegate T FuncSpanT<T>(ReadOnlySpan<char> text);
internal delegate T FuncSpanArrayT<T>(in TextFindHelper finder);

namespace RecordParser.Engines.Reader
{
    internal static class ReaderEngine
    {
        public static Expression<FuncSpanArrayT<T>> RecordParserSpanCSV<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, (i, mapConfig) =>
            {
                return Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(mapConfig.start));
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanArrayT<T>>(body, configParameter);

            return result;
        }

        public static Expression<Func<Foo, T>> RecordParserSpanFlatAOT<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var line = Expression.Parameter(typeof(Foo), "span");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, (i, mapConfig) =>
            {
                return Slice(line, Expression.Constant(mapConfig.start), Expression.Constant(mapConfig.length.Value));
            }, true);

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<Func<Foo, T>>(body, line);

            return result;
        }

        public static Expression<FuncSpanT<T>> RecordParserSpanFlat<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var line = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, (i, mapConfig) =>
            {
                return Slice(line, Expression.Constant(mapConfig.start), Expression.Constant(mapConfig.length.Value));
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanT<T>>(body, line);

            return result;
        }

        private static BlockExpression MountBody<T>(
            ParameterExpression instanceVariable,
            BlockExpression blockThatSetProperties,
            IEnumerable<MappingReadConfiguration> mappedColumns,
            Func<T> factory)
        {
            var getNewInstance = factory != null
                ? Call(factory)
                : CreateInstanceEngine.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop).OfType<MemberExpression>()).Body;

            var assign = Expression.Assign(instanceVariable, getNewInstance);
            var body = Expression.Block(
                typeof(T),
                variables: blockThatSetProperties.Variables.Prepend(instanceVariable),
                expressions: blockThatSetProperties.Expressions.Prepend(assign));

            return body;
        }

        private static BlockExpression MountSetProperties(
            ParameterExpression objectParameter,
            IEnumerable<MappingReadConfiguration> mappedColumns,
            Func<int, MappingReadConfiguration, Expression> getTextValue,
            bool AOT = false)
        {
            var replacer = new ParameterReplacerVisitor(objectParameter);
            var assignsExpressions = new List<Expression>();
            var i = -1;

            Func<Type, Expression, Delegate, Expression> resolve = AOT ? GetValueToBeSetExpressionAOT : GetValueToBeSetExpression;

            foreach (var x in mappedColumns)
            {
                i++;

                Expression textValue = getTextValue(i, x);

                if (x.ShouldTrim)
                    textValue = Trim(textValue);

                var propertyType = x.prop.Type;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
                var isPropertyNullable = nullableUnderlyingType != null;
                var propertyUnderlyingType = nullableUnderlyingType ?? propertyType;

                Expression valueToBeSetExpression = resolve(
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

        private static Expression GetValueToBeSetExpressionAOT(Type propertyType, Expression valueText, Delegate func)
        {
            if (func != null)
            {
                var fun = Expression.Constant(func);

                return Expression.Call(valueText, nameof(Foo.AsCustom), new[] { func.Method.ReturnType }, fun);
            }

            if (propertyType.IsEnum)
            {
                return Expression.Call(valueText, nameof(Foo.AsEnum), new[] { propertyType });
            }

            var methodName = "As" + propertyType.Name;
            var method = typeof(Foo).GetMethod(methodName);

            if (method == null)
                throw new InvalidOperationException($"Type '{propertyType.FullName}' does not have a default parse");

            return Expression.Call(valueText, method);
        }
    }
}