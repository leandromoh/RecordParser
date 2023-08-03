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
        public bool ParallelProcessing { get; set; }
        public bool ContainsQuotedFields { get; set; }
    }

    public static class VariableLengthReaderExtensions
    {
        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, TextReader stream, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(stream, Length, reader.Separator)
                            : () => new RowByLine(stream, Length);

            Func<ReadOnlyMemory<char>, int, T> parser = (memory, i) => reader.Parse(memory.Span);

            ProcessFunc<T> process = options.ParallelProcessing
                ? GetRecordsParallel
                : GetRecordsSequential;

            return process(parser, func, options.HasHeader);
        }
    }
}
