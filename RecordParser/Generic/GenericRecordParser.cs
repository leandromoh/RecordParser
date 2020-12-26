using RecordParser.Parsers;
using System;
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
            ParameterExpression configParameter = Expression.Variable(typeof(ReadOnlySpan<(int, int)>), "config");

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

                var arrayIndex = MakeArray<(int, int)>(configParameter, Expression.Constant(i));

                var propertyType = propertyName.Type;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
                var isPropertyNullable = nullableUnderlyingType != null;
                var propertyUnderlyingType = nullableUnderlyingType ?? propertyType;

                Expression textValue =

                        //propertyType == typeof(string) && func is null

                        //? 
                        //Expression.Call(valueParameter, nameof(string.Substring), Type.EmptyTypes,
                        //Expression.Field(arrayIndex, "Item1"),
                        //Expression.Field(arrayIndex, "Item2"))

                        //:
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

            return Expression.Lambda<FuncTSpanArrayT<T>>(blockExpr,

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

            propertyType = propertyType.IsEnum ? typeof(Enum) : propertyType;

            if (dic.TryGetValue((valueText.Type, propertyType), out var expF))
                return expF(propertyType, valueText);

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

        private static readonly IDictionary<(Type, Type), Func<Type, Expression, Expression>> dic = new Dictionary<(Type, Type), Func<Type, Expression, Expression>>
        {
            [(typeof(string), typeof(string))] = (_, ex) => ex,
            [(typeof(string), typeof(Enum))] = GetEnumParseExpression,
            [(typeof(ReadOnlySpan<char>), typeof(int))] = GetExpressionExpChar(span => int.Parse(span, NumberStyles.Integer, CultureInfo.InvariantCulture)),
            [(typeof(ReadOnlySpan<char>), typeof(DateTime))] = GetExpressionExpChar(span => DateTime.Parse(span, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces)),
            [(typeof(ReadOnlySpan<char>), typeof(string))] = GetExpressionExpChar(span => new string(span)),
            [(typeof(ReadOnlySpan<char>), typeof(decimal))] = GetExpressionExpChar(span => decimal.Parse(span, NumberStyles.Number, CultureInfo.InvariantCulture))
        };

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

        private static Func<Type, Expression, Expression> GetExpressionExpChar<T>(Expression<Func<ReadOnlySpanChar, T>> ex)
        {
            var intTao = new ReadOnlySpanVisitor().Modify(ex);

            return (Type _, Expression valueText) => new ParameterReplacer(valueText).Visit(intTao.Body);
        }

        private static Expression GetExpressionFunc<R>(Func<string, R> f, Expression valueText)
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

        delegate X FuncSpanIntT<X>(ReadOnlySpan<X> span, int index);

        static Expression GetFunc(Delegate f, params Expression[] args)
        {
            return Expression.Call(f.Target is null ? null : Expression.Constant(f.Target), f.Method, args);
        }

        static Expression MakeArray<X>(params Expression[] args)
        {
            return GetFunc((FuncSpanIntT<X>)(Bla), args);
        }

        static X Bla<X>(ReadOnlySpan<X> spanzin, int i) => spanzin[i];
    }
}