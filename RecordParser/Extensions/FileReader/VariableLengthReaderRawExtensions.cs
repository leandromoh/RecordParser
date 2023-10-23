using RecordParser.Engines;
using RecordParser.Engines.Reader;
using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Extensions.ReaderCommon;

namespace RecordParser.Extensions
{
    public delegate string StringPool(ReadOnlySpan<char> text);
    internal delegate void Get(ref TextFindHelper finder, string[] inst, StringPool cache);

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
        private static Get BuildRaw(int collumnCount, bool hasTransform, bool trim)
        {
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");
            var instanceVariable = Expression.Parameter(typeof(string[]), "inst");
            var cacheParameter = Expression.Parameter(typeof(StringPool), "cache");

            var commands = new Expression[collumnCount];
            for (int i = 0; i < collumnCount; i++)
            {
                var arrayAccessExpr = Expression.ArrayAccess(instanceVariable, Expression.Constant(i));
                var getValue = (Expression)Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(i));

                if (trim)
                    getValue = Expression.Call(typeof(MemoryExtensions), "Trim", Type.EmptyTypes, getValue);

                if (hasTransform)
                {
                    getValue = Expression.Invoke(cacheParameter, getValue);
                }
                else
                {
                    getValue = Expression.Call(getValue, "ToString", Type.EmptyTypes);
                }

                commands[i] = Expression.Assign(arrayAccessExpr, getValue);
            }

            var block = Expression.Block(commands);
            var final = Expression.Lambda<Get>(block, configParameter, instanceVariable, cacheParameter);

            return final.Compile();
        }

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
            var get = BuildRaw(options.ColumnCount, options.StringPoolFactory != null, options.Trim);
            var sep = options.Separator;

            Func<IFL> func = options.ContainsQuotedFields
                           ? () => new RowByQuote(reader, Length, sep)
                           : () => new RowByLine(reader, Length);

            var parallelOptions = options.ParallelismOptions ?? new();

            return parallelOptions.Enabled
                    ? GetParallel()
                    : GetSequential();

            IEnumerable<T> GetSequential()
            {
                var buffer = new string[options.ColumnCount];
                var stringCache = options.StringPoolFactory?.Invoke();
                var getField = (int i) => buffer[i];

                return ReadRecordsSequential(Parser, func, options.HasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, sep, QuoteHelper.Quote);

                    try
                    {
                        get(ref finder, buffer, stringCache);

                        return parser(getField);
                    }
                    finally
                    {
                        finder.Dispose();
                    }
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
                            var buf = new string[options.ColumnCount];

                            return new
                            {
                                buffer = buf,
                                lockObj = new object(),
                                stringCache = options.StringPoolFactory?.Invoke(),
                                getField = new Func<int, string>(i => buf[i])
                            };
                        })
                        .ToArray();

                return ReadRecordsParallel(Parser, func, options.HasHeader, parallelOptions);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, sep, QuoteHelper.Quote);

                    try
                    {
                        var r = funcs[i % maxParallelism];
                        lock (r.lockObj)
                        {
                            get(ref finder, r.buffer, r.stringCache);

                            return parser(r.getField);
                        }
                    }
                    finally
                    {
                        finder.Dispose();
                    }
                }
            }
        }
    }
}
