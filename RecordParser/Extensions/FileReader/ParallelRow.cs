﻿using RecordParser.Engines.Reader;
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
    public class VariableLengthReaderOptions
    {
        public bool hasHeader;
        public bool parallelProcessing;
        public bool containsQuotedFields;
    }

    public delegate string StringFactory(ReadOnlySpan<char> text);

    public class VariableLengthReaderRawOptions
    {
        public bool hasHeader;
        public bool parallelProcessing;
        public bool containsQuotedFields;

        public int columnCount;
        public string separator;
        public Func<StringFactory> StringFactory;
    }

    public static partial class Exasd
    {
        private static int length = (int)Math.Pow(2, 23);

        delegate void Get(ref TextFindHelper finder, string[] inst, StringFactory cache);
        private static Get BuildRaw(int collumnCount, bool hasTransform)
        {
            // parameters
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");
            var instanceVariable = Expression.Parameter(typeof(string[]), "inst");
            var cacheParameter = Expression.Parameter(typeof(StringFactory), "cache");

            var commands = new Expression[collumnCount];
            for (int i = 0; i < collumnCount; i++)
            {
                var arrayAccessExpr = Expression.ArrayAccess(instanceVariable, Expression.Constant(i));
                var getValue = (Expression)Expression.Call(configParameter, nameof(TextFindHelper.GetValue), Type.EmptyTypes, Expression.Constant(i));
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

        // ???
        public static IEnumerable<T> GetRecordsRaw<T>(this TextReader stream, VariableLengthReaderRawOptions options, Func<Func<int, string>, T> reader)
        {
            var get = BuildRaw(options.columnCount, options.StringFactory != null);

            Func<IFL> func = options.containsQuotedFields
                           ? () => new QuotedRow(stream, length, options.separator)
                           : () => new RowByLine(stream, length);

            return options.parallelProcessing
                    ? GetParallel()
                    : GetSequential();

            IEnumerable<T> GetSequential()
            {
                var buffer = new string[options.columnCount];
                var stringCache = options.StringFactory?.Invoke();
                var getField = new Func<int, string>(i => buffer[i]);

                return GetRecordsSequential(Parser, func, options.hasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, options.separator, ('"', "\""));

                    try
                    {
                        get(ref finder, buffer, stringCache);

                        return reader(getField);
                    }
                    finally
                    {
                        finder.Dispose();
                    }
                }
            }

            IEnumerable<T> GetParallel()
            {
                var maxParallelism = 20;
                var funcs = Enumerable
                        .Range(0, maxParallelism)
                        .Select(_ =>
                        {
                            var buf = new string[options.columnCount];

                            return new
                            {
                                buffer = buf,
                                lockObj = new object(),
                                stringCache = options.StringFactory?.Invoke(),
                                getField = new Func<int, string>(i => buf[i])
                            };
                        })
                        .ToArray();

                return GetRecordsParallel(Parser, func, options.hasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, options.separator, ('"', "\""));

                    try
                    {
                        var r = funcs[i % maxParallelism];
                        lock (r.lockObj)
                        {

                            get(ref finder, r.buffer, r.stringCache);

                            return reader(r.getField);
                        }
                    }
                    finally
                    {
                        finder.Dispose();
                    }
                }
            }
        }

        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, TextReader stream, VariableLengthReaderOptions options)
        {
            Func<IFL> func = options.containsQuotedFields
                            ? () => new QuotedRow(stream, length, reader.separator)
                            : () => new RowByLine(stream, length);

            Func<ReadOnlyMemory<char>, int, T> parser = (memory, i) => reader.Parse(memory.Span);

            return options.parallelProcessing
                    ? GetRecordsParallel(parser, func, options.hasHeader)
                    : GetRecordsSequential(parser, func, options.hasHeader);
        }
    }
}
