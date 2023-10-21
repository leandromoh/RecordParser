using RecordParser.Builders.Reader;
using RecordParser.Extensions.FileReader.RowReaders;
using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public delegate string StringPool(ReadOnlySpan<char> text);

    public class VariableLengthReaderRawOptions
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
        /// Indicates if field's values should be trimmed.
        /// Default value is false.
        /// </summary>
        public bool Trim { get; set; } = false;

        /// <summary>
        /// Indicates how many columns each record has.
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// The text (usually a character) that delimits columns and separate values.
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// Options to configure parallel processing
        /// </summary>
        public ParallelismOptions ParallelismOptions { get; set; }

        /// <summary>
        /// Factory for string pool instances.
        /// </summary>
        public Func<StringPool> StringPoolFactory { get; set; }
    }

    public static class VariableLengthReaderRawExtensions
    {
        /// <summary>
        /// Reads the records from a variable length file, then parses each record
        /// to object by accessing each field's value by index.
        /// </summary>
        /// <typeparam name="T">type of objects read from file</typeparam>
        /// <param name="reader">variable length file</param>
        /// <param name="options">options to configure the parsing</param>
        /// <param name="parser">parser that receives a function that returns field's value by index</param>
        /// <returns>
        /// Sequence of records.
        /// </returns>
        public static IEnumerable<T> ReadRecordsRaw<T>(this TextReader reader, VariableLengthReaderRawOptions options, Func<Func<int, string>, T> parser)
        {
            Func<IFL> func = options.ContainsQuotedFields
                            ? () => new RowByQuote(reader, Length, options.Separator)
                            : () => new RowByLine(reader, Length);

            var parallelOptions = options.ParallelismOptions ?? new();

            return parallelOptions.Enabled
                ? GetParallel()
                : GetSequential();

            IEnumerable<T> GetSequential()
            {
                var buffer = new string[options.ColumnCount];
                var reader = BuildReader(options.Separator, options.ColumnCount, options.Trim, () => buffer, options.StringPoolFactory);
                var getField = (int i) => buffer[i];

                return ReadRecordsSequential(Parser, func, options.HasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    reader.Parse(memory.Span);
                    return parser(getField);
                }
            }

            IEnumerable<T> GetParallel()
            {
                // TODO remove hardcoded
                var maxParallelism = 20;
                var funcs = Enumerable
                        .Range(0, maxParallelism)
                        .Select(_ =>
                        {
                            var buffer = new string[options.ColumnCount];

                            return new
                            {
                                buffer,
                                lockObj = new object(),
                                reader = BuildReader(options.Separator, options.ColumnCount, options.Trim, () => buffer, options.StringPoolFactory),
                                getField = new Func<int, string>(i => buffer[i]),
                            };
                        })
                        .ToArray();

                return ReadRecordsParallel(Parser, func, options.HasHeader, parallelOptions);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var r = funcs[i % maxParallelism];
                    lock (r.lockObj)
                    {
                        r.reader.Parse(memory.Span);
                        return parser(r.getField);
                    }
                }
            }
        }

        private static IVariableLengthReader<string[]> BuildReader(string separator, int columnCount, bool trim, Func<string[]> factory, Func<StringPool> poolFactory)
        {
            var builder = new VariableLengthReaderSequentialBuilder<string[]>();

            for (var i = 0; i < columnCount; i++)
                builder.Map(buildExpression(i));

            if (poolFactory != null)
            {
                var pool = poolFactory();
                builder.DefaultTypeConvert<string>(trim 
                    ? x => pool(x.Trim()) 
                    : x => pool(x));
            }

            var reader = builder.Build(separator, factory: factory);

            return reader;

            // builds the lambda: array => array[i]
            static Expression<Func<string[], string>> buildExpression(int i)
            {
                var arrayExpr = Expression.Parameter(typeof(string[]));
                var indexExpr = Expression.Constant(i);
                var arrayAccessExpr = Expression.ArrayAccess(arrayExpr, indexExpr);

                return Expression.Lambda<Func<string[], string>>(arrayAccessExpr, arrayExpr);
            }
        }
    }
}
