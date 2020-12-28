using System;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser.Generic
{
    public class ClosureVisitor : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression member)
        {
            var isPrimitive = member.Type.IsValueType || member.Type == typeof(string);

            if (isPrimitive && member.Expression is ConstantExpression constant)
            {
                if (member.Member is FieldInfo fieldInfo)
                {
                    var value = fieldInfo.GetValue(constant.Value);
                    return Expression.Constant(value, member.Type);
                }
                if (member.Member is PropertyInfo propertyInfo)
                {
                    var value = propertyInfo.GetValue(constant.Value);
                    return Expression.Constant(value, member.Type);
                }
            }

            return base.VisitMember(member);
        }
    }
}
