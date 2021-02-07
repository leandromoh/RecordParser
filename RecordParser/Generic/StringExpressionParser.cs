using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Generic.GenericRecordParser;

namespace RecordParser.Generic
{
    internal static class StringExpressionParser
    {
        public static Expression<Func<string[], T>> RecordParser<T>(IEnumerable<MappingConfiguration> mappedColumns)
        {
            var funcThatSetProperties = GetFuncThatSetProperties<T>(mappedColumns);
            var getNewInstance = CreateInstanceHelper.GetInstanceGenerator<T>(mappedColumns.Select(x => x.prop));

            var instanceParameter = funcThatSetProperties.Parameters[0];
            var valueParameter = funcThatSetProperties.Parameters[1];

            var instanceVariable = Expression.Variable(typeof(T), "inst");
            var assign = Expression.Assign(instanceVariable, getNewInstance.Body);
            var body = new ParameterReplacer(instanceVariable, instanceParameter).Visit(funcThatSetProperties.Body);
            var block = body as BlockExpression;

            Expression set = Expression.Block(
                typeof(T),
                variables: block != null ? block.Variables.Prepend(instanceVariable) : new[] { instanceVariable },
                expressions: block != null ? block.Expressions.Prepend(assign) : new[] { assign, body });

            var result = Expression.Lambda<Func<string[], T>>(set, valueParameter);

            return result;
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

            var blockExpr = Expression.Block(assignsExpressions);

            return Expression.Lambda<Func<T, string[], T>>(blockExpr, new[] { objectParameter, valueParameter });
        }

        private static Expression GetIsNullOrWhiteSpaceExpression(Expression valueText)
        {
            Func<string, bool> func = string.IsNullOrWhiteSpace;
            return GetExpressionFunc(func, valueText);
        }
    }
}
