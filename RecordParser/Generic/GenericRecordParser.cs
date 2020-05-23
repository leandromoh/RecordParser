using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public class GenericRecordParser<T>
    {
        private readonly IEnumerable<(MemberExpression prop, Expression func)> _mappedColumns;
        private readonly HashSet<string> _columnsThatIfEmptyShouldParseObjectAsNull;

        private readonly Func<T, string[], T> _funcThatSetProperties;
        private readonly Func<T> _getNewInstance;

        public GenericRecordParser(IEnumerable<(MemberExpression prop, Expression func)> mappedColumns, IEnumerable<string> columnsThatIfEmptyShouldObjectParseAsNull = null)
        {
            foreach (var skipColumn in columnsThatIfEmptyShouldObjectParseAsNull ?? Enumerable.Empty<string>())
                if (!mappedColumns.Any(m => m.prop.Member.Name == skipColumn))
                    throw new ArgumentException($"Property '{skipColumn}' is not mapped in mappedColumns parameter");

            _mappedColumns = mappedColumns;
            _funcThatSetProperties = GetFuncThatSetProperties(mappedColumns);
            _columnsThatIfEmptyShouldParseObjectAsNull = columnsThatIfEmptyShouldObjectParseAsNull?.ToHashSet() ?? new HashSet<string>();
            _getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(_mappedColumns.Select(x => x.prop));
        }

        public T Parse(string[] line)
        {
            T obj = _getNewInstance();

            return Parse(obj, line);
        }

        public T Parse(T obj, string[] values)
        {
            var skipParse = _columnsThatIfEmptyShouldParseObjectAsNull != null && _mappedColumns
                 .Zip(values, (propertyName, value) => (propertyName: propertyName.prop, value))
                 .Any(y => string.IsNullOrWhiteSpace(y.value) && _columnsThatIfEmptyShouldParseObjectAsNull.Contains(y.propertyName.Member.Name));

            return skipParse
                   ? default
                   : _funcThatSetProperties(obj, values);
        }

        private static Func<T, string[], T> GetFuncThatSetProperties(IEnumerable<(MemberExpression prop, Expression func)> mappedColumns)
        {
            ParameterExpression objectParameter = Expression.Variable(typeof(T), "a");
            ParameterExpression valueParameter = Expression.Variable(typeof(string[]), "values");

            var replacer = new ParameterReplacer(objectParameter);
            var assignsExpressions = new List<Expression>();
            var i = 0;

            foreach (var (propertyName, func) in mappedColumns)
            {
                Expression textValue = Expression.ArrayIndex(valueParameter, Expression.Constant(i++));

                if (propertyName is null)
                    continue;

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

            return Expression.Lambda<Func<T, string[], T>>(blockExpr, new[] { objectParameter, valueParameter }).Compile();
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