using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.BuilderWrite.SpanExpressionHelper;

namespace RecordParser.BuilderWrite
{
    public interface IFixedLengthWriterBuilder<T>
    {
        IFixedLengthWriter<T> Build();
        IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format = null, Padding padding = Padding.Right, char paddingChar = ' ');
    }

    public class FixedLengthWriterBuilder<T> : IFixedLengthWriterBuilder<T>
    {
        private readonly List<MappingWriteConfiguration> list = new List<MappingWriteConfiguration>();

        public IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingWriteConfiguration(member, startIndex, length, format, padding, paddingChar, typeof(R), null));
            return this;
        }

        public IFixedLengthWriter<T> Build()
        {
            var expression = GetFuncThatSetProperties(list);

            return new FixedLengthWriter<T>(expression.Compile());
        }

        private static Expression<FuncSpanTInt<T>> GetFuncThatSetProperties(IEnumerable<MappingWriteConfiguration> mappedColumns)
        {
            // parameters
            ParameterExpression span = Expression.Variable(typeof(Span<char>), "span");
            ParameterExpression inst = Expression.Variable(typeof(T), "inst");

            var replacer = new ParameterReplacerVisitor(inst);

            // variables
            ParameterExpression charsWritten = Expression.Variable(typeof(int), "charsWritten");
            ParameterExpression temp = Expression.Variable(typeof(Span<char>), "tempSpan");

            List<ParameterExpression> variables = new List<ParameterExpression>();
            variables.Add(charsWritten);
            variables.Add(temp);

            // commands
            List<Expression> commands = new List<Expression>();

            LabelTarget returnTarget = Expression.Label(typeof(int));
            GotoExpression gotoReturn = Expression.Return(returnTarget, Expression.Constant(0));

            var necessarySpace = Expression.Constant(mappedColumns.Max(x => x.start + x.length.Value));

            var toLarge = Expression.LessThan(
                                Expression.PropertyOrField(span, "Length"),
                                necessarySpace);

            commands.Add(Expression.IfThen(toLarge, gotoReturn));

            //  var i = -1;
            foreach (var map in mappedColumns)
            {
                commands.Add(
                    Expression.Assign(temp, Slice(span, map.start, map.length.Value)));

                var prop = replacer.Visit(map.prop);

                DAs(prop, map, commands, temp, charsWritten, gotoReturn);

                CallPad(map);
            }

            commands.Add(Expression.Return(returnTarget, necessarySpace));

            commands.Add(Expression.Label(returnTarget, Expression.Constant(0)));


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
                        Slice(temp, 0, charsWritten),
                        temp,
                        Expression.Constant(map.paddingChar)));
            }
        }
    }

    internal static class SpanExpressionHelper
    {

        public static void DAs(Expression prop, MappingWriteConfiguration map, List<Expression> commands, ParameterExpression temp, ParameterExpression charsWritten, GotoExpression gotoReturn)
        {
            if (prop.Type.IsEnum)
            {
                prop = Expression.Call(prop, "ToString", Type.EmptyTypes);
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
                    temp, charsWritten, format, Expression.Constant(map.formatProvider, typeof(IFormatProvider)));

                commands.Add(Expression.IfThen(Expression.Not(tryFormat), gotoReturn));
            }
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
