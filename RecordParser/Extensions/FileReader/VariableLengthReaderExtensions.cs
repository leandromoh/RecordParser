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
        public bool hasHeader;
        public bool parallelProcessing;
        public bool containsQuotedFields;
    }

    public static class VariableLengthReaderExtensions
    {
        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, TextReader stream, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.containsQuotedFields
                            ? () => new RowByQuote(stream, Length, reader.separator)
                            : () => new RowByLine(stream, Length);

            Func<ReadOnlyMemory<char>, int, T> parser = (memory, i) => reader.Parse(memory.Span);

            return options.parallelProcessing
                    ? GetRecordsParallel(parser, func, options.hasHeader)
                    : GetRecordsSequential(parser, func, options.hasHeader);
        }
    }
}
