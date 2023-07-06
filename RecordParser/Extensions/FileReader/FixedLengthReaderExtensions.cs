using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public class FixedLengthReaderOptions<T>
    {
        public bool parallelProcessing;
        public Func<ReadOnlyMemory<char>, int, T> parser;
    }

    public static class FixedLengthReaderExtensions
    {
        public static IEnumerable<T> GetRecords<T>(this TextReader stream, FixedLengthReaderOptions<T> options)
        {
            Func<IFL> func = () => new RowByLine(stream, Length);
            ProcessFunc<T> process = GetProcessFunc<T>(options.parallelProcessing);

            return process(options.parser, func, hasHeader: false);
        }

        public static IEnumerable<ReadOnlyMemory<char>> GetRecords(this TextReader stream)
        {
            return GetRecordsSequential((memory, i) => memory, () => new RowByLine(stream, Length), hasHeader: false);
        }
    }
}
