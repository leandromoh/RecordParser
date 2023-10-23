using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.ReaderCommon;

namespace RecordParser.Extensions
{
    public class FixedLengthReaderOptions<T>
    {
        /// <summary>
        /// Options to configure parallel processing
        /// </summary>
        public ParallelismOptions ParallelismOptions { get; set; }
        /// <summary>
        /// Function which parses text into object
        /// </summary>
        public FuncSpanT<T> Parser { get; set; }
    }

    public static class FixedLengthReaderExtensions
    {
        private const bool HasHeader = false;

        /// <summary>
        /// Reads the records (i.e., lines) from a fixed length file, 
        /// then parses the records into objects.
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="reader">fixed length file</param>
        /// <param name="options">options to configure the parsing</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        public static IEnumerable<T> ReadRecords<T>(this TextReader reader, FixedLengthReaderOptions<T> options)
        {
            var func = () => new RowByLine(reader, Length);
            var parser = (ReadOnlyMemory<char> memory, int i) => options.Parser(memory.Span);
            var parallelOptions = options.ParallelismOptions ?? new();

            return
                parallelOptions.Enabled
                ? ReadRecordsParallel(parser, func, HasHeader, parallelOptions)
                : ReadRecordsSequential(parser, func, HasHeader);
        }

        /// <summary>
        /// Reads the records (i.e., lines) from a fixed length file.
        /// The records are returned in the order they are in the file.
        /// </summary>
        /// <param name="reader">fixed length file</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        /// <remarks>
        /// The ReadOnlyMemory instances representing the records points to regions of the internal buffer used to read the file.
        /// Store ReadOnlyMemory values will not hold record's values since the content of the buffer changes
        /// as it goes forward through the file.
        /// </remarks>
        public static IEnumerable<ReadOnlyMemory<char>> ReadRecords(this TextReader reader)
        {
            return ReadRecordsSequential((memory, i) => memory, () => new RowByLine(reader, Length), HasHeader);
        }
    }
}
