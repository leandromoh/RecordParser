using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.BuilderWrite
{
    public interface IFixedLengthWriterSequentialBuilder<T>
    {
        IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null);
        IFixedLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        IFixedLengthWriterSequentialBuilder<T> Skip(int length);

        IFixedLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ');
        IFixedLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ');
    }

    public class FixedLengthWriterSequentialBuilder<T> : IFixedLengthWriterSequentialBuilder<T>
    {
        private readonly IFixedLengthWriterBuilder<T> indexed = new FixedLengthWriterBuilder<T>();
        private int currentPosition = 0;

        public IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null) 
            => indexed.Build(cultureInfo);

        public IFixedLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public IFixedLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            indexed.Map(ex, currentPosition, length, format, padding, paddingChar);
            currentPosition += length;
            return this;
        }

        public IFixedLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            indexed.Map(ex, currentPosition, length, converter, padding, paddingChar);
            currentPosition += length;
            return this;
        }

        public IFixedLengthWriterSequentialBuilder<T> Skip(int length)
        {
            currentPosition += length;
            return this;
        }
    }
}
