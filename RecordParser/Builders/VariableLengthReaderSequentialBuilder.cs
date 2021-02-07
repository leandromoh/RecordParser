using System;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReaderSequentialBuilder<T>
    {
        IVariableLengthReader<T> Build(string separator);
        IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        IVariableLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, Expression<Func<string, R>> convert = null);
        IVariableLengthReaderSequentialBuilder<T> Skip(int columnCount);
    }

    public class VariableLengthReaderSequentialBuilder<T> : IVariableLengthReaderSequentialBuilder<T>
    {
        private readonly IVariableLengthReaderBuilder<T> indexed = new VariableLengthReaderBuilder<T>();
        private int currentIndex = -1;

        public IVariableLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex,
            Expression<Func<string, R>> convert = null)
        {
            indexed.Map(ex, ++currentIndex, convert);
            return this;
        }

        public IVariableLengthReaderSequentialBuilder<T> Skip(int columnCount)
        {
            currentIndex += columnCount;
            return this;
        }

        public IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public IVariableLengthReader<T> Build(string separator) => indexed.Build(separator);
    }
}
