using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public class FixedLengthReaderOptions<T>
    {
        public ParallelOptions ParallelOptions { get; set; }
        public FuncSpanT<T> Parser { get; set; }
    }

    public static class FixedLengthReaderExtensions
    {
        private const bool HasHeader = false;
        
        public static IEnumerable<T> GetRecords<T>(this TextReader stream, FixedLengthReaderOptions<T> options)
        {
            var func = () => new RowByLine(stream, Length);
            var parser = (ReadOnlyMemory<char> memory, int i) => options.Parser(memory.Span);
            var parallelOptions = options.ParallelOptions ?? new();

            return
                parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, HasHeader);
        }

        public static IEnumerable<ReadOnlyMemory<char>> GetRecords(this TextReader stream)
        {
            return GetRecordsSequential((memory, i) => memory, () => new RowByLine(stream, Length), HasHeader);
        }
    }
}
