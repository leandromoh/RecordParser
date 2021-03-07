using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.BuilderWrite.SpanExpressionHelper;

namespace RecordParser.BuilderWrite
{
    public interface IFixedLengthWriterBuilder<T>
    {
        IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null);
        IFixedLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ');
        IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ');
    }

    public class FixedLengthWriterBuilder<T> : IFixedLengthWriterBuilder<T>
    {
        private readonly List<MappingWriteConfiguration> list = new();
        private readonly Dictionary<Type, Func<Expression, Expression, Expression, Expression>> dic = new();

        public IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingWriteConfiguration(member, startIndex, length, converter.WrapInLambdaExpression(), null, padding, paddingChar, typeof(R)));
            return this;

        }
        public IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingWriteConfiguration(member, startIndex, length, null, format, padding, paddingChar, typeof(R)));
            return this;
        }

        public IFixedLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null)
        {
            var maps = MappingWriteConfiguration.Merge(list, dic);
            var expression = GetFuncThatSetProperties(maps, cultureInfo);

            return new FixedLengthWriter<T>(expression.Compile());
        }

        private static Expression<FuncSpanTInt<T>> GetFuncThatSetProperties(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
        {
            // parameters
            ParameterExpression span = Expression.Variable(typeof(Span<char>), "span");
            ParameterExpression inst = Expression.Variable(typeof(T), "inst");

            var replacer = new ParameterReplacerVisitor(inst);

            // variables
            ParameterExpression offset = Expression.Variable(typeof(int), "charsWritten");
            ParameterExpression temp = Expression.Variable(typeof(Span<char>), "tempSpan");

            List<ParameterExpression> variables = new List<ParameterExpression>();
            variables.Add(offset);
            variables.Add(temp);

            // commands
            List<Expression> commands = new List<Expression>();

            LabelTarget returnTarget = Expression.Label(typeof((bool, int)));

            var necessarySpace = Expression.Constant(mappedColumns.Max(x => x.start + x.length.Value));

            var tooShort = Expression.LessThan(
                                Expression.PropertyOrField(span, "Length"),
                                necessarySpace);

            var charsWritten = Expression.Constant(0);

            commands.Add(Expression.IfThen(tooShort, GetReturn(false, charsWritten)));

            foreach (var map in mappedColumns)
            {
                commands.Add(
                    Expression.Assign(temp, Slice(span, map.start, map.length.Value)));

                var prop = replacer.Visit(map.prop);

                var gotoReturn = map.converter == null && prop.Type == typeof(string)
                    ? GetReturn(false, charsWritten)
                    : GetReturn(false, Expression.Add(charsWritten, offset));

                DAs(prop, map, commands, temp, offset, gotoReturn, cultureInfo);

                CallPad(map);

                charsWritten = Expression.Constant(map.start + map.length.Value);
            }

            commands.Add(GetReturn(true, necessarySpace));

            commands.Add(Expression.Label(returnTarget, Expression.Constant(default((bool, int)))));


            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanTInt<T>>(blockExpr, new[] { span, inst });

            return lambda;

            void CallPad(MappingWriteConfiguration map)
            {
                var padFunc = map.padding == Padding.Left
                    ? nameof(PadLeft)
                    : nameof(PadRight);

                commands.Add(
                    Expression.Call(typeof(SpanExpressionHelper), padFunc, Type.EmptyTypes,
                        Slice(temp, 0, offset),
                        temp,
                        Expression.Constant(map.paddingChar)));
            }

            Expression GetReturn(bool success, Expression countWritten)
            {
                var returnValue = Expression.New(
                                typeof((bool, int)).GetConstructor(new[] { typeof(bool), typeof(int) }),
                                Expression.Constant(success), countWritten);

                return Expression.Return(returnTarget, returnValue);
            }
        }
    }

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

                var tryFormat =
                    Expression.Call(prop, "TryFormat", Type.EmptyTypes,
                    temp, charsWritten, format, Expression.Constant(cultureInfo, typeof(CultureInfo)));

                commands.Add(Expression.IfThen(Expression.Not(tryFormat), gotoReturn));
            }
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
                        Expression.Call(
                            Expression.Convert(enumValue, under), "TryFormat", Type.EmptyTypes, span, charsWritten,
                                Expression.Default(typeof(ReadOnlySpan<char>)), Expression.Constant(null, typeof(CultureInfo)))
                        ,
                        Expression.New(
                                typeof((bool, int)).GetConstructor(new[] { typeof(bool), typeof(int) }),
                                Expression.Constant(true), charsWritten),

                        Expression.New(
                                typeof((bool, int)).GetConstructor(new[] { typeof(bool), typeof(int) }),
                                Expression.Constant(false), charsWritten)),

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

        public static Span<char> PadRight(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total);

            for (int i = span.Length, totalWidth = total.Length; i < totalWidth; i++)
                total[i] = paddingChar;

            return total;
        }

        public static Span<char> PadLeft(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total.Slice(diff));

            for (var i = 0; i < diff; i++)
                total[i] = paddingChar;

            return total;
        }
    }
}
