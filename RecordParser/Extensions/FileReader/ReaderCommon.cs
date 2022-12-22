using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Extensions.FileReader
{
    internal static class ReaderCommon
    {
        public static readonly int Length = (int)Math.Pow(2, 23);

        public static IEnumerable<T> GetRecordsParallel<T>(Func<ReadOnlyMemory<char>, int, T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().AsOrdered().Select(reader))
            {
                yield return x;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine().AsParallel().AsOrdered().Select(reader))
                {
                    yield return x;
                }
            }
        }

        public static IEnumerable<T> GetRecordsSequential<T>(Func<ReadOnlyMemory<char>, int, T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            var i = 0;
            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
            {
                yield return reader(x, i++);
            }

            while (items.FillBufferAsync() > 0)
            {
                i = 0;
                foreach (var x in items.TryReadLine())
                {
                    yield return reader(x, i++);
                }
            }
        }
    }
}
