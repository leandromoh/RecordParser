using RecordParser.Engines.Reader;
using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Extensions
{
    public class VariableLengthReaderOptions
    {
        public bool hasHeader;
        public bool parallelProcessing;
        public bool containsQuotedFields;
    }

    public static partial class Exasd
    {
        private static int length = (int)Math.Pow(2, 23);

        private interface IFL : IDisposable
        {
            int FillBufferAsync();
            IEnumerable<Memory<char>> TryReadLine();
        }


        delegate void Get(ref TextFindHelper finder, string[] inst, FuncSpanT<string> cache);

        private static Get BuildRaw(int collumnCount, bool hasTransform)
        {
            // parameters
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");
            var instanceVariable = Expression.Parameter(typeof(string[]), "inst");
            var cacheParameter = Expression.Parameter(typeof(FuncSpanT<string>), "cache");

            var commands = new Expression[collumnCount];
            for (int i = 0; i < collumnCount; i++)
            {
                var arrayAccessExpr = Expression.ArrayAccess(instanceVariable, Expression.Constant(i));
                var getValue = (Expression)Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(i));
                getValue = Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, getValue);


                if (hasTransform)
                {
                    getValue = Expression.Invoke(cacheParameter, getValue);
                }
                else
                {
                    getValue = Expression.Call(getValue, "ToString", Type.EmptyTypes);
                }

                commands[i] = Expression.Assign(arrayAccessExpr, getValue);
            }

            var block = Expression.Block(commands);
            var final = Expression.Lambda<Get>(block, configParameter, instanceVariable, cacheParameter);

            return final.Compile();
        }

        // ???
        public static IEnumerable<T> GetRecordsParallelRaw<T>(this TextReader stream, int columnCount, bool hasHeader, Func<Func<int, string>, T> reader, Func<FuncSpanT<string>> stringCache = null)
        {
            var get = BuildRaw(columnCount, stringCache != null);

            var parallelism = 20;
            var funcs = Enumerable
                    .Range(0, parallelism)
                    .Select(_ =>
                    {
                        var buf = new string[columnCount];

                        return new
                        {
                            buffer = buf,
                            lockObj = new object(),
                            stringCache = stringCache != null ? stringCache() : null,
                            getField = new Func<int, string>(i => buf[i])
                        };
                    })
                    .ToArray();

            using var items = new RowByLine(stream, length);

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var item in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().AsOrdered().Select(Parse))
            {
                yield return item;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var item in items.TryReadLine().AsParallel().AsOrdered().Select(Parse))
                {
                    yield return item;
                }
            }

            T Parse(Memory<char> memory, int i)
            {
                var finder = new TextFindHelper(memory.Span, ",", ('"', "\""));

                try
                {
                    var r = funcs[i % parallelism];
                    lock (r.lockObj)
                    {

                        get(ref finder, r.buffer, r.stringCache);

                        return reader(r.getField);
                    }
                }
                finally
                {
                    finder.Dispose();
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

        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, TextReader stream, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.containsQuotedFields
                            ? () => new QuotedRow(stream, length)
                            : () => new RowByLine(stream, length);

            return options.parallelProcessing
                    ? GetRecordsParallel(reader.Parse, func, options.hasHeader)
                    : GetRecordsSequential(reader.Parse, func, options.hasHeader);
        }

        //// 46
        //public static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        //{
        //    return GetRecordsParallel(reader.Parse, () => new RowByLine(stream, length), hasHeader);
        //}

        //// 125
        //public static IEnumerable<T> GetRecordsSequential<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        //{
        //    return GetRecordsSequential(reader.Parse, () => new RowByLine(stream, length), hasHeader);
        //}

        //// 61
        //public static IEnumerable<T> GetRecordsParallelCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        //{
        //    return GetRecordsParallel(reader.Parse, () => new QuotedRow(stream, length), hasHeader);
        //}

        //// 130
        //public static IEnumerable<T> GetRecordsSequentialCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        //{
        //    return GetRecordsSequential(reader.Parse, () => new QuotedRow(stream, length), hasHeader);
        //}

        private static IEnumerable<T> GetRecordsParallel<T>(FuncSpanT<T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().AsOrdered().Select(x => reader(x.Span)))
            {
                yield return x;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine().AsParallel().AsOrdered().Select(x => reader(x.Span)))
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
