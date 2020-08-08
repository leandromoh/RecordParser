﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public static class GenericRecordParser
    {
        public static Expression<Func<string[], T>> RecordParser<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            var funcThatSetProperties = GetFuncThatSetProperties<T>(mappedColumns);
            var getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop));
            var shouldSkip = GetShouldSkip(mappedColumns);

            var instanceParameter = funcThatSetProperties.Parameters[0];
            var valueParameter = funcThatSetProperties.Parameters[1];

            var instanceVariable = Expression.Variable(typeof(T), "inst");
            var assign = Expression.Assign(instanceVariable, getNewInstance.Body);
            var body = new ParameterReplacer(instanceVariable, instanceParameter).Visit(funcThatSetProperties.Body);

            Expression set = Expression.Block(
                typeof(T),
                variables: new[] { instanceVariable },
                expressions: body is BlockExpression block
                     ? block.Expressions.Prepend(assign)
                     : new[] { assign, body });

            if (shouldSkip is { })
            {
                set = Expression.Condition(
                            test: Expression.Invoke(shouldSkip, valueParameter),
                            ifTrue: Expression.Default(typeof(T)),
                            ifFalse: set);
            }

            var result = Expression.Lambda<Func<string[], T>>(set, valueParameter);

            return result;
        }

        public static Expression<Func<string, (int, int)[], T>> RecordParserSpan<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            var funcThatSetProperties = GetFuncThatSetPropertiesSpan<T>(mappedColumns);
            var getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop));
            var shouldSkip = GetShouldSkip(mappedColumns);

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

            if (shouldSkip is { })
            {
                set = Expression.Condition(
                            test: Expression.Invoke(shouldSkip, valueParameter),
                            ifTrue: Expression.Default(typeof(T)),
                            ifFalse: set);
            }

            var result = Expression.Lambda<Func<string, (int, int)[], T>>(set, valueParameter, configParameter);

            return result;
        }

        public static Expression<Func<T, string, (int start, int length)[], T>> GetFuncThatSetPropertiesSpan<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression valueParameter = Expression.Variable(typeof(string), "value");
            ParameterExpression configParameter = Expression.Variable(typeof((int start, int length)[]), "config");

            var span = Expression.Variable(typeof(ReadOnlySpan<char>), "span");
            var like = typeof(MemoryExtensions).GetMethod(nameof(MemoryExtensions.AsSpan), new[] { typeof(string) });

            var replacer = new ParameterReplacer(objectParameter);
            var assignsExpressions = new List<Expression>()
            {
                Expression.Assign(span, Expression.Call(null, like, valueParameter))
            };

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

                Expression textValue = propertyType == typeof(string) && func is null
                    
                        ? 
                        Expression.Call(valueParameter, nameof(string.Substring), Type.EmptyTypes,
                        Expression.Field(arrayIndex, "Item1"),
                        Expression.Field(arrayIndex, "Item2"))
                        
                        :
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
                        test: GetIsNullOrWhiteSpaceExpression(textValue),
                        ifTrue: Expression.Default(propertyType),
                        ifFalse: valueToBeSetExpression);
                }

                var assign = Expression.Assign(replacer.Visit(propertyName), valueToBeSetExpression);

                assignsExpressions.Add(assign);
            }

            assignsExpressions.Add(objectParameter);

            var blockExpr = Expression.Block(typeof(T), new[] { span }, assignsExpressions);

            return Expression.Lambda<Func<T, string, (int start, int length)[], T>>(blockExpr, 
                
                new[] { objectParameter, valueParameter, configParameter });
        }


        private static Expression<Func<string[], bool>> GetShouldSkip(IEnumerable<MappingConfiguration> columns)
        {
            ParameterExpression valueParameter = Expression.Variable(typeof(string[]), "values");
            var constitions = new List<Expression>();
            var i = -1;

            foreach (var map in columns)
            {
                i++;
                if (map.skipWhen == null) continue;
                var valueText = Expression.ArrayIndex(valueParameter, Expression.Constant(i));
                var validation = Expression.Invoke(map.skipWhen, valueText);
                constitions.Add(validation);
            }

            if (constitions.Count == 0)
                return null;

            var validations = constitions.Aggregate((acc, x) => Expression.OrElse(acc, x));

            return Expression.Lambda<Func<string[], bool>>(validations, valueParameter);
        }

        private static Expression<Func<T, string[], T>> GetFuncThatSetProperties<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression valueParameter = Expression.Variable(typeof(string[]), "values");

            var replacer = new ParameterReplacer(objectParameter);
            var assignsExpressions = new List<Expression>();
            var i = -1;

            foreach (var x in mappedColumns)
            {
                i++;

                if (x.prop is null)
                    continue;

                Expression textValue = Expression.ArrayIndex(valueParameter, Expression.Constant(i));

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
                        test: GetIsNullOrWhiteSpaceExpression(textValue),
                        ifTrue: Expression.Constant(null, propertyType),
                        ifFalse: valueToBeSetExpression);
                }

                var assign = Expression.Assign(replacer.Visit(x.prop), valueToBeSetExpression);

                assignsExpressions.Add(assign);
            }

            assignsExpressions.Add(objectParameter);

            var blockExpr = Expression.Block(typeof(T), assignsExpressions);

            return Expression.Lambda<Func<T, string[], T>>(blockExpr, new[] { objectParameter, valueParameter });
        }

        private static Expression GetValueToBeSetExpression(Type propertyType, Expression valueText, Expression func)
        {
            if (func != null)
                if (func is LambdaExpression lamb)
                    return new ParameterReplacer(valueText).Visit(lamb.Body);
                else
                    return Expression.Invoke(func, valueText);

            if (propertyType == typeof(string))
                return valueText;

            if (propertyType.IsEnum)
                return GetEnumParseExpression(propertyType, valueText);

            return GetParseExpression(propertyType, valueText);
        }

        private static Expression GetEnumParseExpression(Type type, Expression valueText)
        {
            return GetExpressionExp(text =>
                Enum.Parse(type, text.Replace(" ", string.Empty), true), valueText);
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

        private static Expression GetIsNullOrWhiteSpaceExpression(Expression valueText)
        {
            return GetExpressionFunc(string.IsNullOrWhiteSpace, valueText);
        }

        private static Expression GetIsWhiteSpaceExpression(Expression valueText)
        {
            return Expression.Call(typeof(MemoryExtensions), 
                nameof(MemoryExtensions.IsWhiteSpace), 
                Type.EmptyTypes, valueText);
        }

        private static Expression GetExpressionExp<R>(Expression<Func<string, R>> f, Expression valueText)
        {
            return new ParameterReplacer(valueText).Visit(f.Body);
        }

        private static Expression GetExpressionFunc<R>(Func<string, R> f, Expression valueText)
        {
            return Expression.Call(f.Target is null ? null : Expression.Constant(f.Target), f.Method, valueText);
        }

        public static IEnumerable<MappingConfiguration> Merge(
            IEnumerable<MappingConfiguration> list,
            IReadOnlyDictionary<Type, Expression> dic)
        {
            if (dic?.Any() != true)
                return list;

            var result = list
                .Select(i =>
                {
                    var fmask = i.fmask ?? (dic.TryGetValue(i.type, out var ex) ? ex : null);
                    return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask, i.skipWhen);
                })
                .ToList();

            return result;
        }
    }

    public struct ReadOnlySpanChar
    {
        public static implicit operator ReadOnlySpan<char>(ReadOnlySpanChar _) => default;
    }
}