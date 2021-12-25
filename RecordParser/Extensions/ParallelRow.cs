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

        // ???
        public static IEnumerable<T> GetRecordsParallelRaw<T>(this TextReader stream, int columnCount, bool hasHeader, Func<Func<int, string>, T> reader, Func<FuncSpanT<string>> stringCache = null)
        {
            var parallelism = 30;
            var funcs = Enumerable
                    .Range(0, parallelism)
                    .Select(_ =>
                    {
                        return new CSVRawReader(columnCount, stringCache());
                    })
                    .ToArray();

            using var items = new QuotedRow(stream, length);

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var item in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(Parse))
            {
                yield return item;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var item in items.TryReadLine().AsParallel().Select(Parse))
                {
                    yield return item;
                }
            }

            T Parse(Memory<char> memory, int i)
            {
                var r = funcs[i % parallelism];
                lock (r._lockObj)
                {
                    r._reader.Parse(memory.Span);

                    return reader(r.GetField);
                }
            }
        }

        //public static List<T> GetRecordsParallelRaw_2<T>(this TextReader stream, int columnCount, bool hasHeader, Func<Func<int, string>, T> reader, Func<FuncSpanT<string>> stringCache = null)
        //{

        //    var parallelism = 20;
        //    var funcs = Enumerable
        //            .Range(0, parallelism)
        //            .Select(_ =>
        //            {
        //                var reader = new CSVRawReader(columnCount, stringCache());
        //                return (reader, lockObj: new object());
        //            })
        //            .ToArray();

        //    return foo()
        //        .AsParallel()
        //        .Select((item, i) =>
        //        {
        //            var (f, locFunc) = funcs[i % parallelism];

        //            lock (locFunc)
        //            {
        //                f._reader.Parse(item.Span);

        //                return reader(f.GetField);
        //            }
        //        })
        //        .ToList();

        //    IEnumerable<Memory<char>> foo()
        //    {
        //        using var items = new QuotedRow(stream, (int)Math.Pow(2, 15));

        //        if (items.FillBufferAsync() > 0 == false)
        //        {
        //            yield break;
        //        }

        //        foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
        //        {
        //            yield return x;
        //        }

        //        while (items.FillBufferAsync() > 0)
        //        {
        //            foreach (var x in items.TryReadLine())
        //            {
        //                yield return x;
        //            }
        //        }
        //    }
        //}


        // 46
        public static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return GetRecordsParallel(reader.Parse, () => new RowByLine(stream, length), hasHeader);
        }

        // 125
        public static IEnumerable<T> GetRecordsSequential<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return GetRecordsSequential(reader.Parse, () => new RowByLine(stream, length), hasHeader);
        }

        // 61
        public static IEnumerable<T> GetRecordsParallelCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return GetRecordsParallel(reader.Parse, () => new QuotedRow(stream, length), hasHeader);
        }

        // 130
        public static IEnumerable<T> GetRecordsSequentialCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            return GetRecordsSequential(reader.Parse, () => new QuotedRow(stream, length), hasHeader);
        }

        private static IEnumerable<T> GetRecordsParallel<T>(FuncSpanT<T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(x => reader(x.Span)))
            {
                yield return x;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine().AsParallel().Select(x => reader(x.Span)))
                {
                    yield return x;
                }
            }
        }

        private static IEnumerable<T> GetRecordsSequential<T>(FuncSpanT<T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
            {
                yield return reader(x.Span);
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine())
                {
                    yield return reader(x.Span);
                }
            }
        }
    }
}
