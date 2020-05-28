using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public class GenericRecordParser<T>
    {
        private readonly Func<T, string[], T> _funcThatSetProperties;
        private readonly Func<string[], bool> _shouldSkip;
        private readonly Func<T> _getNewInstance;

        public GenericRecordParser(IEnumerable<MappingConfiguration> mappedColumns)
        {
            _funcThatSetProperties = GetFuncThatSetProperties(mappedColumns).Compile();
            _getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop)).Compile();
            _shouldSkip = GetShouldSkip(mappedColumns)?.Compile();
        }

        public T Parse(string[] values)
        {
            if (_shouldSkip != null && _shouldSkip(values))
                return default;

            T obj = _getNewInstance();

            return _funcThatSetProperties(obj, values);
        }

        public T Parse(T obj, string[] values)
        {
            if (_shouldSkip != null && _shouldSkip(values))
                return default;

            return _funcThatSetProperties(obj, values);
        }

        private static Expression<Func<string[], bool>> GetShouldSkip(IEnumerable<MappingConfiguration> columns)
        {
            ParameterExpression valueParameter = Expression.Variable(typeof(string[]), "values");
            var constitions = new List<Expression>();
            var i = -1;

            foreach(var map in columns)
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

        private static Expression<Func<T, string[], T>> GetFuncThatSetProperties(IEnumerable<MappingConfiguration> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression valueParameter = Expression.Variable(typeof(string[]), "values");

            var replacer = new ParameterReplacer(objectParameter);
            var assignsExpressions = new List<Expression>();
            var i = -1;

            foreach (var x in mappedColumns)
            {
                i++;
                var (propertyName, func) = (x.prop, x.fmask);

                if (propertyName is null)
                    continue;

                Expression textValue = Expression.ArrayIndex(valueParameter, Expression.Constant(i));

                var propertyType = propertyName.Type;
                var nullableUnderlyingType = Nullable.GetUnderlyingType(propertyType);
                var isPropertyNullable = nullableUnderlyingType != null;
                var propertyUnderlyingType = nullableUnderlyingType ?? propertyType;

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

            var blockExpr = Expression.Block(typeof(T), new ParameterExpression[] { }, assignsExpressions);

            return Expression.Lambda<Func<T, string[], T>>(blockExpr, new[] { objectParameter, valueParameter });
        }

        private static Expression GetValueToBeSetExpression(Type propertyType, Expression valueText, Expression func)
        {
            if (func != null)
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
            return GetExpressionExp(text =>
                Convert.ChangeType(text, type, CultureInfo.InvariantCulture), valueText);
        }

        private static Expression GetIsNullOrWhiteSpaceExpression(Expression valueText)
        {
            return GetExpressionExp(text =>
                string.IsNullOrWhiteSpace(text), valueText);
        }

        private static Expression GetExpressionExp<R>(Expression<Func<string, R>> f, Expression valueText)
        {
            return Expression.Invoke(f, valueText);
        }

        private static Expression GetExpressionFunc<R>(Func<string, R> f, Expression valueText)
        {
            return Expression.Call(Expression.Constant(f.Target), f.Method, valueText);
        }
    }
}