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
        /// <summary>
        /// Indicates if there is a header record present in the reader's content.
        /// If true, the first record (the header) will be skipped.
        /// Default value is false, so nothing is skipped by default.
        /// </summary>
        public bool HasHeader { get; set; } = false;
        /// <summary>
        /// Indicates if there are any quoted field in the reader's content.
        /// Default value is true.
        /// </summary>
        public bool ContainsQuotedFields { get; set; } = true;
        /// <summary>
        /// Options to configure parallel processing
        /// </summary>
        public ParallelOptions ParallelOptions { get; set; }
    }

    public static class VariableLengthReaderExtensions
    {
        /// <summary>
        /// Reads the records from a variable length file then parses each record
        /// from text to object
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="stream">variable length file</param>
        /// <param name="reader">parse reader</param>
        /// <param name="options">options to configure the parsing</param>
        /// <returns>
        /// Sequence of records from the file
        /// </returns>
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
