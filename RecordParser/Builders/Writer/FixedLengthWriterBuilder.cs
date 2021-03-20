using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
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
            var expression = FixedLengthWriterEngine.GetFuncThatSetProperties<T>(maps, cultureInfo);

            return new FixedLengthWriter<T>(expression.Compile());
        }
    }
}
