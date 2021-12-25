using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecordParser.Extensions
{
    public static partial class Exasd
	{
        private static int length = (int)Math.Pow(2, 23);

        private interface IFL : IDisposable
        {
            int FillBufferAsync();
            IEnumerable<Memory<char>> TryReadLine();
        }

        // 46
        public static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return reader.GetRecordsParallel(() => new RowByLine(stream, length), hasHeader);
        }

        // 125
        public static IEnumerable<T> GetRecordsSequential<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return reader.GetRecordsSequential(() => new RowByLine(stream, length), hasHeader);
        }

        // 61
        public static IEnumerable<T> GetRecordsParallelCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return reader.GetRecordsParallel(() => new QuotedRow(stream, length), hasHeader);
        }

        // 130
        public static IEnumerable<T> GetRecordsSequentialCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return reader.GetRecordsSequential(() => new QuotedRow(stream, length), hasHeader);
        }

        private static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, Func<IFL> getItems, bool hasHeader)
		{
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(x => reader.Parse(x.Span)))
            {
                yield return x;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine().AsParallel().Select(x => reader.Parse(x.Span)))
                {
                    yield return x;
                }
            }
        }

        private static IEnumerable<T> GetRecordsSequential<T>(this IVariableLengthReader<T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
            {
                yield return reader.Parse(x.Span);
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine())
                {
                    yield return reader.Parse(x.Span);
                }
            }
        }
    }
}
