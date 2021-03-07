using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser.BuilderWrite
{
    internal static class SpanExpressionHelper
    {
        public static void DAs(Expression prop, MappingWriteConfiguration map, List<Expression> commands, ParameterExpression temp, ParameterExpression charsWritten, Expression gotoReturn, CultureInfo cultureInfo)
        {
            if (map.converter != null)
            {
                var toLarge = map.converter(temp, prop, charsWritten);
                commands.Add(Expression.IfThen(toLarge, gotoReturn));
                return;
            }

            if (prop.Type.IsEnum)
            {
                var result = Expression.Variable(typeof((bool, int)), "temp");

                var toLarge = Expression.Block(variables: new[] { result },
                    Expression.Assign(result, TryFormatEnum(prop, temp)),
                    Expression.Assign(charsWritten, Expression.PropertyOrField(result, "Item2")),
                    Expression.Not(Expression.PropertyOrField(result, "Item1")));

                commands.Add(Expression.IfThen(toLarge, gotoReturn));
                return;
            }

            if (prop.Type == typeof(string))
            {
                var strSpan = StringAsSpan(prop);

                var toLarge = Expression.GreaterThan(
                        Expression.PropertyOrField(prop, "Length"),
                        Expression.PropertyOrField(temp, "Length"));

                commands.Add(Expression.IfThen(toLarge, gotoReturn));

                commands.Add(
                    Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, temp));

                commands.Add(
                    Expression.Assign(charsWritten, Expression.PropertyOrField(prop, "Length")));
            }
            else
            {
                var format = map.format is null
                    ? Expression.Default(typeof(ReadOnlySpan<char>))
                    : StringAsSpan(Expression.Constant(map.format));

                var tryFormat = prop.Type switch
                {
                    _ when prop.Type == typeof(char) || prop.Type == typeof(bool) =>
                        Expression.Call(typeof(SpanExpressionHelper), "TryFormat", Type.EmptyTypes, prop, temp, charsWritten),

                    _ when prop.Type == typeof(Guid) => 
                        Expression.Call(prop, "TryFormat", Type.EmptyTypes, temp, charsWritten, format),

                    _ => Expression.Call(prop, "TryFormat", Type.EmptyTypes, temp, charsWritten, format,
                            Expression.Constant(cultureInfo, typeof(CultureInfo)))
                };

                commands.Add(Expression.IfThen(Expression.Not(tryFormat), gotoReturn));
            }
        }


        public static bool TryFormat(this char c, Span<char> span, out int written)
        {
            if (span.Length > 0)
            {
                span[0] = c;
                written = 1;
                return true;
            }

            written = 0;
            return false;
        }

        public static bool TryFormat(this bool b, Span<char> span, out int written)
        {
            if (b && span.Length > 3)
            {
                span[0] = 'T';
                span[1] = 'r';
                span[2] = 'u';
                span[3] = 'e';

                written = 4;
                return true;
            }

            if (!b && span.Length > 4)
            {
                span[0] = 'F';
                span[1] = 'a';
                span[2] = 'l';
                span[3] = 's';
                span[4] = 'e';

                written = 5;
                return true;
            }

            written = 0;
            return false;
        }

        private static readonly ConstructorInfo _boolIntTupleConstructor;

        static SpanExpressionHelper()
        {
            _boolIntTupleConstructor = typeof((bool, int)).GetConstructor(new[] { typeof(bool), typeof(int) });
        }

        private static NewExpression CreateTuple(bool success, Expression countWritten)
        {
            Debug.Assert(countWritten.Type == typeof(int));

            return Expression.New(_boolIntTupleConstructor, Expression.Constant(success), countWritten);
        }

        public static Expression GetReturn(bool success, Expression countWritten, LabelTarget returnTarget)
        {
            var returnValue = CreateTuple(success, countWritten);

            return Expression.Return(returnTarget, returnValue);
        }

        private static Expression TryFormatEnum(Expression enumValue, Expression span)
        {
            var type = enumValue.Type;

            Debug.Assert(type.IsEnum);
            Debug.Assert(span.Type == typeof(Span<char>));

            var under = Enum.GetUnderlyingType(type);
            var charsWritten = Expression.Variable(typeof(int), "charsWritten");

            var body = Enum.GetValues(type)
                .Cast<object>()
                .Select(color =>
                {
                    var text = color.ToString();

                    var valueEquals = Expression.Equal(enumValue, Expression.Constant(color, type));

                    var enoughSpace = Expression.GreaterThanOrEqual(
                        Expression.PropertyOrField(span, "Length"),
                        Expression.Constant(text.Length));

                    var strSpan = StringAsSpan(Expression.Constant(text));

                    var ifTrue = Expression.Block(
                        Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, span),
                        Expression.Constant((true, text.Length)));

                    var ifFalse = Expression.Constant((false, 0));

                    var block = Expression.Condition(enoughSpace, ifTrue, ifFalse);

                    return (value: block, condition: valueEquals);
                })
                .Reverse()
                .Aggregate(Expression.Condition(
                        test: Expression.Call(
                               Expression.Convert(enumValue, under), "TryFormat", Type.EmptyTypes, span, charsWritten,
                                Expression.Default(typeof(ReadOnlySpan<char>)), Expression.Constant(null, typeof(CultureInfo))),
                        ifTrue: CreateTuple(true, charsWritten),
                        ifFalse: CreateTuple(false, charsWritten)),

                            (acc, item) =>
                                Expression.Condition(
                                    item.condition,
                                    item.value,
                                    acc));

            var blockExpr = Expression.Block(
                                variables: new[] { charsWritten },
                                expressions: body);

            return blockExpr;
        }

        public static Expression StringAsSpan(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "AsSpan", Type.EmptyTypes, str);

        public static Expression Slice(Expression span, int start, int length) =>
            Slice(span, start, Expression.Constant(length));

        public static Expression Slice(Expression span, int start, Expression length) =>
            Expression.Call(span, "Slice", Type.EmptyTypes, Expression.Constant(start), length);
    }
}
