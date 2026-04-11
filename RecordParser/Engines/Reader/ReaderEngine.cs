using RecordParser.Builders.Reader;
using RecordParser.Engines.Reader;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Engines.ExpressionHelper;

public delegate T FuncSpanT<T>(ReadOnlySpan<char> text);
internal delegate T FuncSpanTSafe<T>(ReadOnlySpan<char> text, Action<Exception, int> exceptionHandler);

internal delegate T FuncSpanArrayT<T>(in TextFindHelper finder);
internal delegate T FuncSpanArrayTSafe<T>(in TextFindHelper finder, Action<Exception, int> exceptionHandler);

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

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, mapConfig =>
            {
                return Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(mapConfig.start));
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanArrayT<T>>(body, configParameter);

            return result;
        }

        public static Expression<FuncSpanArrayTSafe<T>> RecordParserSpanCSVSafe<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");
            var exceptionHandler = Expression.Parameter(typeof(Action<Exception, int>), "exceptionHandler");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, mapConfig =>
            {
                return Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(mapConfig.start));
            },
            (mapConfig, assign) =>
            {
                var visitor = new TryCatchVisitor(exceptionHandler, mapConfig.start);
                var result = visitor.Visit(assign);
                return result;
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanArrayTSafe<T>>(body, configParameter, exceptionHandler);

            return result;
        }

        public static Expression<FuncSpanT<T>> RecordParserSpanFlat<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var line = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, mapConfig =>
            {
                return Slice(line, Expression.Constant(mapConfig.start), Expression.Constant(mapConfig.length.Value));
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanT<T>>(body, line);

            return result;
        }

        public static Expression<FuncSpanTSafe<T>> RecordParserSpanFlatSafe<T>(IEnumerable<MappingReadConfiguration> mappedColumns, Func<T> factory)
        {
            // parameters
            var line = Expression.Parameter(typeof(ReadOnlySpan<char>), "span");
            var exceptionHandler = Expression.Parameter(typeof(Action<Exception, int>), "exceptionHandler");

            // variables
            var instanceVariable = Expression.Variable(typeof(T), "inst");

            var blockThatSetProperties = MountSetProperties(instanceVariable, mappedColumns, mapConfig =>
            {
                return Slice(line, Expression.Constant(mapConfig.start), Expression.Constant(mapConfig.length.Value));
            },
            (mapConfig, assign) =>
            {
                var visitor = new TryCatchVisitor(exceptionHandler, mapConfig.start);
                var result = visitor.Visit(assign);
                return result;
            });

            var body = MountBody(instanceVariable, blockThatSetProperties, mappedColumns, factory);
            var result = Expression.Lambda<FuncSpanTSafe<T>>(body, line, exceptionHandler);

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
            Func<MappingReadConfiguration, Expression> getTextValue,
            Func<MappingReadConfiguration, BinaryExpression, Expression> assignHandler = null)
        {
            var replacer = new ParameterReplacerVisitor(objectParameter);
            var assignsExpressions = new List<Expression>();

            foreach (var x in mappedColumns)
            {
                Expression textValue = getTextValue(x);

                if (x.ShouldTrim)
                    textValue = Trim(textValue);

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

                assignsExpressions.Add(assignHandler?.Invoke(x, assign) ?? assign);
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