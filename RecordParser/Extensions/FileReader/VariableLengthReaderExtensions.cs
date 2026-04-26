using RecordParser.Builders.Reader;
using RecordParser.Extensions.FileReader.RowReaders;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using static RecordParser.Extensions.ReaderCommon;

namespace RecordParser.Extensions
{
    public record class VariableLengthReaderAutoBindOptions : VariableLengthReaderOptions
    {
        /// <summary>
        /// The text (usually a character) that delimits columns and separate values
        /// If value is null then separator will be detected automatically by observing the header.
        /// Default value is null.
        /// </summary>
        public string Separator { get; set; } = null;

        /// <summary>
        /// If true then header columns without a matching property or field will simply be ignored;
        /// otherwise, an exception will be thrown indicating the non-matching column.
        /// Default value is true.
        /// </summary>
        public bool SkipUnmatchedColumns { get; set; } = true;
    }

    public record class VariableLengthReaderOptions
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
        /// then parses the records to objects.
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

        /// <summary>
        /// Reads the records from a variable length file
        /// using the header of the file to auto-bind columns to properties in the process of parsing the records to objects. 
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="reader">variable length file</param>
        /// <param name="options">options to configure the parsing</param>
        /// <param name="action">callback to set additional bind or default type converters</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If the header is missing on <paramref name="reader"/> or <paramref name="options"/> indicating that header is absent.
        /// </exception>
        public static IEnumerable<T> ReadRecords<T>(this
            TextReader reader,
            VariableLengthReaderAutoBindOptions options,
            Action<IVariableLengthReaderSequentialBuilder<T>> action = null)
        {
            string header;

            if (!options.HasHeader || string.IsNullOrWhiteSpace(header = reader.ReadLine()))
                throw new InvalidOperationException("Header is mandatory when using auto-binding overload.");

            var separator = options.Separator ?? DetectSeparator(header.AsMemory());
            var columns = header.Split(separator);
            var parser = BuildParser(columns, separator, options.SkipUnmatchedColumns, action);

            return ReadRecords(reader, parser, options with { HasHeader = false });
        }

        private static IVariableLengthReader<T> BuildParser<T>(IEnumerable<string> columns, string separator, bool skipMismatchedColumns, Action<IVariableLengthReaderSequentialBuilder<T>> action)
        {
            var builder = new VariableLengthReaderSequentialBuilder<T>();
            foreach (var column in columns)
            {
                try
                {
                    var cleaned = column
                        .Replace("_", string.Empty)
                        .Replace("'", string.Empty)
                        .Replace("\"", string.Empty)
                        .Trim();

                    dynamic expression = CreateExpression<T>(cleaned);
                    builder.Map(expression);
                }
                catch when (skipMismatchedColumns)
                {
                    builder.Skip(1);
                }
            }

            action?.Invoke(builder);

            return builder.Build(separator);
        }

        private static LambdaExpression CreateExpression<T>(string propertyName)
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression body = param;
            var parts = propertyName.Split('.');

            foreach (var part in parts)
            {
                // starts in T, then loop recursively
                var currentType = body.Type;

                var member = (MemberInfo)
                    currentType.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
                    currentType.GetField(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (member == null)
                    throw new ArgumentException($"Can not bind column '{propertyName}': Type '{currentType.Name}' does not have property or field '{part}'.");

                body = Expression.MakeMemberAccess(body, member);
            }

            return Expression.Lambda(body, param);
        }

        internal static string DetectSeparator(ReadOnlyMemory<char> header)
        {
            var candidates = new string[] { ",", ";", "\t", "|", ":" };
            var headerSpan = header.Span;
            var bestCandidate = ",";
            var maxCount = 0;

            foreach (var candidate in candidates)
            {
                var currentCount = 0;
                var candidateSpan = candidate.AsSpan();
                var candidateLen = candidateSpan.Length;

                if (candidateLen == 0 || candidateLen > headerSpan.Length)
                    continue;

                for (var i = 0; i <= headerSpan.Length - candidateLen; i++)
                {
                    if (headerSpan.Slice(i, candidateLen).SequenceEqual(candidateSpan))
                    {
                        currentCount++;
                        // avoid overlap
                        i += candidateLen - 1;
                    }
                }

                if (currentCount > maxCount)
                {
                    maxCount = currentCount;
                    bestCandidate = candidate;
                }
            }

            return bestCandidate;
        }
    }
}
