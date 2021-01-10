﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Generic.GenericRecordParser;

public delegate T FuncSpanT<T>(ReadOnlySpan<char> text);
public delegate T FuncSpanArrayT<T>(ReadOnlySpan<char> line, (int, int)[] config);
public delegate T FuncTSpanArrayT<T>(T instance, ReadOnlySpan<char> line, (int, int)[] config);

namespace RecordParser.Generic
{
    internal static class SpanExpressionParser
    {
        public static Expression<FuncSpanArrayT<T>> RecordParserSpan<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            var funcThatSetProperties = GetFuncThatSetPropertiesSpan<T>(mappedColumns);
            var getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop));

            var instanceParameter = funcThatSetProperties.Parameters[0];
            var valueParameter = funcThatSetProperties.Parameters[1];
            var configParameter = funcThatSetProperties.Parameters[2];

            var instanceVariable = Expression.Variable(typeof(T), "inst");
            var assign = Expression.Assign(instanceVariable, getNewInstance.Body);
            var body = new ParameterReplacer(instanceVariable, instanceParameter).Visit(funcThatSetProperties.Body);
            var block = body as BlockExpression;

            Expression set = Expression.Block(
                typeof(T),
                variables: block != null ? block.Variables.Prepend(instanceVariable) : new[] { instanceVariable },
                expressions: block != null ? block.Expressions.Prepend(assign) : new[] { assign, body });

            var result = Expression.Lambda<FuncSpanArrayT<T>>(set, valueParameter, configParameter);

            return result;
        }

        public static Expression<FuncTSpanArrayT<T>> GetFuncThatSetPropertiesSpan<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression valueParameter = Expression.Variable(typeof(ReadOnlySpan<char>), "span");
            ParameterExpression configParameter = Expression.Variable(typeof((int start, int length)[]), "config");

            var span = valueParameter;

            var replacer = new ParameterReplacer(objectParameter);
            var assignsExpressions = new List<Expression>();

            var i = -1;

            foreach (var x in mappedColumns)
            {
                i++;
                var (propertyName, func) = (x.prop, x.fmask);

                if (propertyName is null)
                    continue;


                var arrayIndex = Expression.ArrayIndex(configParameter, Expression.Constant(i));

                var propertyType = propertyName.Type;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
                var isPropertyNullable = nullableUnderlyingType != null;
                var propertyUnderlyingType = nullableUnderlyingType ?? propertyType;

                Expression textValue =
                        Expression.Call(span, nameof(ReadOnlySpan<char>.Slice), Type.EmptyTypes,
                        Expression.Field(arrayIndex, "Item1"),
                        Expression.Field(arrayIndex, "Item2"));

                Expression valueToBeSetExpression = GetValueToBeSetExpression(
                                                        propertyUnderlyingType,
                                                        textValue,
                                                        func);

                if (valueToBeSetExpression.Type != propertyType)
                {
                    valueToBeSetExpression = Expression.Convert(valueToBeSetExpression, propertyType);
                }

                if (isPropertyNullable)
                {
                    valueToBeSetExpression = Expression.Condition(
                        test: GetIsWhiteSpaceExpression(textValue),
                        ifTrue: Expression.Constant(null, propertyType),
                        ifFalse: valueToBeSetExpression);
                }

                var assign = Expression.Assign(replacer.Visit(propertyName), valueToBeSetExpression);

                assignsExpressions.Add(assign);
            }

            assignsExpressions.Add(objectParameter);

            var blockExpr = Expression.Block(assignsExpressions);

            return Expression.Lambda<FuncTSpanArrayT<T>>(blockExpr, new[] { objectParameter, valueParameter, configParameter });
        }

        private static Expression GetIsWhiteSpaceExpression(Expression valueText)
        {
            return Expression.Call(typeof(MemoryExtensions),
                nameof(MemoryExtensions.IsWhiteSpace),
                Type.EmptyTypes, valueText);
        }
    }
}
