﻿using System;
using System.Collections.Generic;
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

        private static Expression<FuncTSpanArrayT<T>> GetFuncThatSetPropertiesSpan<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression span = Expression.Variable(typeof(ReadOnlySpan<char>), "span");
            ParameterExpression configParameter = Expression.Variable(typeof(ReadOnlySpan<(int start, int length)>), "config");

            var blockExpr = MountSetProperties(objectParameter, mappedColumns, i =>
            {
                var arrayIndex = ReadOnlySpanIndex<(int, int)>(configParameter, Expression.Constant(i));

                var textValue =
                    Expression.Call(span, nameof(ReadOnlySpan<char>.Slice), Type.EmptyTypes,
                    Expression.Field(arrayIndex, "Item1"),
                    Expression.Field(arrayIndex, "Item2"));

                return Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, textValue);
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
            static T func(ReadOnlySpan<T> span, int i) => span[i];

            return GetExpressionFunc((FuncSpanIntT<T>)func, args);
        }
    }
}