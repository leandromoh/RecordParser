using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public interface IVariableLengthWriterSequentialBuilder<T>
    {
        IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null);
        IVariableLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        IVariableLengthWriterSequentialBuilder<T> Skip(int columnCount);

        IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, string format);
        IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, FuncSpanTIntBool<R> converter = null);
    }

    public class VariableLengthWriterSequentialBuilder<T> : IVariableLengthWriterSequentialBuilder<T>
    {
        private readonly IVariableLengthWriterBuilder<T> indexed = new VariableLengthWriterBuilder<T>();
        private int currentIndex = -1;

        public IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null)
            => indexed.Build(separator, cultureInfo);

        public IVariableLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, string format)
        {
            indexed.Map(ex, ++currentIndex, format);
            return this;
        }

        public IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, FuncSpanTIntBool<R> converter = null)
        {
            indexed.Map(ex, ++currentIndex, converter);
            return this;
        }

        public IVariableLengthWriterSequentialBuilder<T> Skip(int columnCount)
        {
            currentIndex += columnCount;
            return this;
        }
    }
}
