using System;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public class CSVSequentialBuilder<T>
    {
        private readonly CSVIndexedBuilder<T> indexed = new CSVIndexedBuilder<T>();
        private int currentIndex = -1;

        public CSVSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            indexed.Map(ex, ++currentIndex, convert, skipRecordWhen);
            return this;
        }

        public CSVSequentialBuilder<T> Skip(int collumCount)
        {
            currentIndex += collumCount;
            return this;
        }

        public CSVSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public CSVReader<T> Build() => indexed.Build();
    }
}
