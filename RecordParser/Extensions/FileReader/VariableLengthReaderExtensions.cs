using RecordParser.Extensions.FileReader.RowReaders;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
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
    }
}
