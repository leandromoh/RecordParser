using System;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ICSVSequentialBuilder<T>
    {
        ICSVReader<T> Build(string separator = ";");
        ICSVSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        ICSVSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, Expression<Func<string, R>> convert = null, Expression<Func<string, bool>> skipRecordWhen = null);
        ICSVSequentialBuilder<T> Skip(int collumCount);
    }

    public class CSVSequentialBuilder<T> : ICSVSequentialBuilder<T>
    {
        private readonly ICSVIndexedBuilder<T> indexed = new CSVIndexedBuilder<T>();
        private int currentIndex = -1;

        public ICSVSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            indexed.Map(ex, ++currentIndex, convert, skipRecordWhen);
            return this;
        }

        public ICSVSequentialBuilder<T> Skip(int collumCount)
        {
            currentIndex += collumCount;
            return this;
        }

        public ICSVSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public ICSVReader<T> Build(string separator) => indexed.Build(separator);
    }
}
