using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Visitors
{
    internal class CultureInfoVisitor : ExpressionVisitor
    {
        private readonly ConstantExpression cultureExpression;

        public CultureInfoVisitor(CultureInfo cultureInfo)
        {
            cultureExpression = Expression.Constant(cultureInfo, typeof(CultureInfo));
        }

        public static T ReplaceCulture<T>(T expression, CultureInfo cultureInfo)
            where T : Expression
        {
            if (cultureInfo == null)
                return expression;

            return (T) new CultureInfoVisitor(cultureInfo).Visit(expression);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type == typeof(IFormatProvider))
                return cultureExpression;

            return base.VisitConstant(node);
        }
    }
}
