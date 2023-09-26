using RecordParser.Engines.Reader;
using RecordParser.Extensions.FileReader.RowReaders;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public class VariableLengthReaderOptions
    {
        public bool HasHeader { get; set; }
        public bool ContainsQuotedFields { get; set; }
        public ParallelOptions ParallelOptions { get; set; }
        // TODO create ParallelOptionsSafe
    }

    public static class VariableLengthReaderExtensions
    {
        // maybe to remove reader parameter and adds what we need from it
        // inside options parameter
        public static IEnumerable<T> GetRecords<T>(this TextReader stream, IVariableLengthReader<T> reader, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(stream, Length, reader.Separator)
                            : () => new RowByLine(stream, Length);

            var parser = (ReadOnlyMemory<char> memory, int i) => reader.Parse(memory.Span);
            var parallelOptions = options.ParallelOptions ?? new();

            return parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, options.HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, options.HasHeader);
        }

        // new overload that returns record itself
        public static IEnumerable<ReadOnlyMemory<char>> GetRecords(this TextReader stream, VariableLengthReaderOptions options, string separator)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(stream, Length, separator)
                            : () => new RowByLine(stream, Length);

            var parser = (ReadOnlyMemory<char> memory, int i) => memory;
            var parallelOptions = options.ParallelOptions ?? new();

            return parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, options.HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, options.HasHeader);
        }


        // new overload that gives something like 'finder.GetField' to user do whatever he whats
        // what about to wrapper TextFindHelper with another struct that only wrappers 'finder.GetField'?
        public static IEnumerable<T> GetRecordsFast<T>(this TextReader stream, VariableLengthReaderOptions options, string separator, Parse<T> getValue)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(stream, Length, separator)
                            : () => new RowByLine(stream, Length);

            var parser = (ReadOnlyMemory<char> memory, int i) =>
            {
                var fi = new TextFindHelper(memory.Span, separator, ('"', "\""));

                return getValue(fi, i);
            };

            var parallelOptions = options.ParallelOptions ?? new();

            return parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, options.HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, options.HasHeader);
        }

        // new overload that gives something like 'finder.GetField' to user do whatever he whats
        public static IEnumerable<T> GetRecordsFast2<T>(this TextReader stream, VariableLengthReaderRawOptions options, string separator, Func<Func<int, string>, T> getValue)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(stream, Length, separator)
                            : () => new RowByLine(stream, Length);

            var caches = Enumerable.Range(0, 10).Select(_ =>
            {
                var buffer = new string[options.ColumnCount];
                return new
                {
                    cache = options.StringPoolFactory?.Invoke(),
                    buffer = buffer,
                    func = new Func<int, string>(i => buffer[i])
                };
            }).ToArray();

            var parser = (ReadOnlyMemory<char> memory, int i) =>
            {
                // memento here?
                var fi = new TextFindHelper(memory.Span, separator, ('"', "\""));

                var cache = caches[i % caches.Length];
                lock (cache)
                {
                    for (int j = 0; j < cache.buffer.Length; j++)
                    {
                        if (cache.cache is null)
                            cache.buffer[j] = fi.GetValue(j).ToString();
                        else
                            cache.buffer[j] = cache.cache(fi.GetValue(j));
                    }
                    return getValue(cache.func);
                }
            };

        var parallelOptions = options.ParallelOptions ?? new();

            return parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, options.HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, options.HasHeader);
    }
}

public delegate T Parse<T>(TextFindHelper index, int i);
}
