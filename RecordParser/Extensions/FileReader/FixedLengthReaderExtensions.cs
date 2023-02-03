using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public class FixedLengthReaderOptions
    {
        public bool parallelProcessing;
        public Func<ReadOnlyMemory<char>, int, object> parser;
    }

    public static class FixedLengthReaderExtensions
    {
        public static IEnumerable<object> GetRecords(this TextReader stream, FixedLengthReaderOptions options)
        {
            Func<IFL> func = () => new RowByLine(stream, Length);

            return options.parallelProcessing
                    ? GetRecordsParallel(options.parser, func, hasHeader: false)
                    : GetRecordsSequential(options.parser, func, hasHeader: false);
        }

        public static IEnumerable<ReadOnlyMemory<char>> GetRecords(this TextReader stream)
        {
            return GetRecordsSequential((memory, i) => memory, () => new RowByLine(stream, Length), hasHeader: false);
        }
    }
}
