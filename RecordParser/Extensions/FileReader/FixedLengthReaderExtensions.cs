using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public class FixedLengthReaderOptions<T>
    {
        /// <summary>
        /// Options to configure parallel processing
        /// </summary>
        public ParallelOptions ParallelOptions { get; set; }
        /// <summary>
        /// Parse function which transforms text to object
        /// </summary>
        public FuncSpanT<T> Parser { get; set; }
    }

    public static class FixedLengthReaderExtensions
    {
        private const bool HasHeader = false;

        /// <summary>
        /// Reads the records (i.e., lines) from a fixed length file then parses each record
        /// from text to object.
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="reader">fixed length file</param>
        /// <param name="options">options to configure the parsing</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        public static IEnumerable<T> GetRecords<T>(this TextReader reader, FixedLengthReaderOptions<T> options)
        {
            var func = () => new RowByLine(reader, Length);
            var parser = (ReadOnlyMemory<char> memory, int i) => options.Parser(memory.Span);
            var parallelOptions = options.ParallelOptions ?? new();

            return
                parallelOptions.Enabled
                ? GetRecordsParallel(parser, func, HasHeader, parallelOptions)
                : GetRecordsSequential(parser, func, HasHeader);
        }

        /// <summary>
        /// Reads the records (i.e., lines) from a fixed length file.
        /// The records are returned in the order they are in the file
        /// </summary>
        /// <param name="reader">fixed length file</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        /// <remarks>
        /// The ReadOnlyMemory instances representing the records points to regions of the internal buffer used to read the file.
        /// Store ReadOnlyMemory values will not hold record's values since the content of the buffer changes
        /// as it goes forward through the file
        /// </remarks>
        public static IEnumerable<ReadOnlyMemory<char>> GetRecords(this TextReader reader)
        {
            return GetRecordsSequential((memory, i) => memory, () => new RowByLine(reader, Length), HasHeader);
        }
    }
}
