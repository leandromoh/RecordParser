using RecordParser.Builders.Writer;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static RecordParser.Engines.ExpressionHelper;

namespace RecordParser.Engines.Writer
{
    internal static class WriteEngine
    {
        public static Expression DAs(Expression prop, MappingWriteConfiguration map, ParameterExpression temp, ParameterExpression charsWritten, Expression gotoReturn, CultureInfo cultureInfo)
        {
            if (map.converter != null)
            {
                if (map.type == typeof(ReadOnlySpan<char>))
                {
                    prop = StringAsSpan(prop);
                }

                var result = Expression.Variable(typeof((bool, int)), "temp");

                var toLarge = Expression.Block(variables: new[] { result },
                    Expression.Assign(result, Call(map.converter, temp, prop)),
                    Expression.Assign(charsWritten, Expression.PropertyOrField(result, "Item2")),
                    Expression.Not(Expression.PropertyOrField(result, "Item1")));

                return Expression.IfThen(toLarge, gotoReturn);
            }

            var isNullableT = prop.Type.IsValueType &&
                              Nullable.GetUnderlyingType(prop.Type) is not null;
            if (isNullableT)
            {
                var hasValue = Expression.PropertyOrField(prop, "HasValue");
                var value = Expression.PropertyOrField(prop, "Value");

                var ifTrue = Expression.Assign(charsWritten, Expression.Constant(0));
                var ifFalse = DAs(value, map, temp, charsWritten, gotoReturn, cultureInfo);

                return Expression.IfThenElse(Expression.Not(hasValue), ifTrue, ifFalse);
            }

            if (prop.Type.IsEnum)
            {
                var result = Expression.Variable(typeof((bool, int)), "temp");

                var toLarge = Expression.Block(variables: new[] { result },
                    Expression.Assign(result, TryFormatEnum(prop, temp)),
                    Expression.Assign(charsWritten, Expression.PropertyOrField(result, "Item2")),
                    Expression.Not(Expression.PropertyOrField(result, "Item1")));

                return Expression.IfThen(toLarge, gotoReturn);
            }

            if (prop.Type == typeof(string))
            {
                var strSpan = StringAsSpan(prop);

                var toLarge = Expression.GreaterThan(
                        Expression.PropertyOrField(prop, "Length"),
                        Expression.PropertyOrField(temp, "Length"));

                return Expression.Block(
                    Expression.IfThen(toLarge, gotoReturn),
                    Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, temp),
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
                        Expression.Call(typeof(WriteEngine), "TryFormat", Type.EmptyTypes, prop, temp, charsWritten),

                    _ when prop.Type == typeof(Guid) =>
                        Expression.Call(prop, "TryFormat", Type.EmptyTypes, temp, charsWritten, format),

                    _ => Expression.Call(prop, "TryFormat", Type.EmptyTypes, temp, charsWritten, format,
                            Expression.Constant(cultureInfo, typeof(CultureInfo)))
                };

                return Expression.IfThen(Expression.Not(tryFormat), gotoReturn);
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

        static WriteEngine()
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
                        test: Expression.Call(typeof(WriteEngine), nameof(TryFormatEnumFallback), new[] { type }, enumValue, span, charsWritten),
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

        private static bool TryFormatEnumFallback<TEnum>(TEnum value, Span<char> destination, out int charsWritten)
            where TEnum : struct, Enum
        {
            var text = value.ToString();

            if (destination.Length < text.Length)
            {
                charsWritten = 0;
                return false;
            }

            text.AsSpan().CopyTo(destination);

            charsWritten = text.Length;
            return true;
        }
    }
}