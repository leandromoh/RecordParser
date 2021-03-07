using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.BuilderWrite.SpanExpressionHelper;
using static RecordParser.BuilderWrite.FixedLengthPadWriter;

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

            commands.Add(Expression.IfThen(tooShort, GetReturn(false, charsWritten, returnTarget)));

            foreach (var map in mappedColumns)
            {
                commands.Add(
                    Expression.Assign(temp, Slice(span, map.start, map.length.Value)));

                var prop = replacer.Visit(map.prop);

                var gotoReturn = map.UseTryPattern
                    ? GetReturn(false, Expression.Add(charsWritten, offset), returnTarget)
                    : GetReturn(false, charsWritten, returnTarget);

                DAs(prop, map, commands, temp, offset, gotoReturn, cultureInfo);

                CallPad(map);

                charsWritten = Expression.Constant(map.start + map.length.Value);
            }

            commands.Add(GetReturn(true, necessarySpace, returnTarget));

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
                    Expression.Call(typeof(FixedLengthPadWriter), padFunc, Type.EmptyTypes,
                        Slice(temp, 0, offset),
                        temp,
                        Expression.Constant(map.paddingChar)));
            }
        }
    }
}
