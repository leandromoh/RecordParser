using RecordParser.Builders.Reader;
using RecordParser.Parsers;
using System;
using System.Linq.Expressions;

namespace RecordParser.Extensions
{
    internal class CSVRawReader
    {
        public readonly IVariableLengthReader<string[]> _reader;
        public readonly object _lockObj;
        public readonly Func<int, string> GetField;

        public CSVRawReader(int columnCount) : this(columnCount, null)
        {
        }

        public CSVRawReader(int columnCount, FuncSpanT<string> stringCache)
        {
            var builder = new VariableLengthReaderSequentialBuilder<string[]>();
            var buffer = new string[columnCount];

            for (var i = 0; i < columnCount; i++)
                builder.Map(BuildExpression(i));

            if (stringCache != null)
                builder.DefaultTypeConvert(stringCache);

            _reader = builder.Build(",", factory: () => buffer);
            _lockObj = new object();
            GetField = (int i) => buffer[i];
        }

        private static Expression<Func<string[], string>> BuildExpression(int i)
        {
            var arrayExpr = Expression.Parameter(typeof(string[]));
            var indexExpr = Expression.Constant(i);
            var arrayAccessExpr = Expression.ArrayAccess(arrayExpr, indexExpr);

            return Expression.Lambda<Func<string[], string>>(arrayAccessExpr, arrayExpr);
        }
    }
}
