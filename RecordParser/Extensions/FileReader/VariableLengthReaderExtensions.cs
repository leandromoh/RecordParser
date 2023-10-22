using RecordParser.Extensions.FileReader.RowReaders;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using static RecordParser.Extensions.ReaderCommon;

namespace RecordParser.Extensions
{
    public class VariableLengthReaderOptions
    {
        /// <summary>
        /// Indicates if there is a header record present in the reader's content.
        /// If true, the first record (the header) will be skipped.
        /// Default value is false, so nothing is skipped by default.
        /// </summary>
        public bool HasHeader { get; set; } = false;
        /// <summary>
        /// Indicates if there are any quoted field in the reader's content.
        /// If false, some optimizations might be applied.
        /// Default value is true.
        /// </summary>
        public bool ContainsQuotedFields { get; set; } = true;
        /// <summary>
        /// Options to configure parallel processing
        /// </summary>
        public ParallelismOptions ParallelismOptions { get; set; }
    }

    public static class VariableLengthReaderExtensions
    {
        /// <summary>
        /// Reads the records from a variable length file, 
        /// then parses the records into objects.
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="reader">variable length file</param>
        /// <param name="parser">parsing reader</param>
        /// <param name="options">options to configure the parsing</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        public static IEnumerable<T> ReadRecords<T>(this TextReader reader, IVariableLengthReader<T> parser, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(reader, Length, parser.Separator)
                            : () => new RowByLine(reader, Length);

            var selector = (ReadOnlyMemory<char> memory, int i) => parser.Parse(memory.Span);
            var parallelOptions = options.ParallelismOptions ?? new();

            return parallelOptions.Enabled
                ? ReadRecordsParallel(selector, func, options.HasHeader, parallelOptions)
                : ReadRecordsSequential(selector, func, options.HasHeader);
        }
    }
}
