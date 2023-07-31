﻿using RecordParser.Engines.Reader;
using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Extensions.FileReader.ReaderCommon;

namespace RecordParser.Extensions.FileReader
{
    public delegate string StringFactory(ReadOnlySpan<char> text);
    internal delegate void Get(ref TextFindHelper finder, string[] inst, StringFactory cache);

    public class VariableLengthReaderRawOptions
    {
        public bool HasHeader;
        public bool ParallelProcessing;
        public bool ContainsQuotedFields;
        public bool Trim;

        public int ColumnCount;
        public string Separator;
        public Func<StringFactory> StringFactory;
    }

    public static class VariableLengthReaderRawExtensions
    {
        private static readonly (char, string) _quote = ('"', "\"");

        private static Get BuildRaw(int collumnCount, bool hasTransform, bool trim)
        {
            var configParameter = Expression.Parameter(typeof(TextFindHelper).MakeByRefType(), "config");
            var instanceVariable = Expression.Parameter(typeof(string[]), "inst");
            var cacheParameter = Expression.Parameter(typeof(StringFactory), "cache");

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

        public static IEnumerable<T> GetRecordsRaw<T>(this TextReader stream, VariableLengthReaderRawOptions options, Func<Func<int, string>, T> reader)
        {
            var get = BuildRaw(options.ColumnCount, options.StringFactory != null, options.Trim);

            Func<IFL> func = options.ContainsQuotedFields
                           ? () => new RowByQuote(stream, Length, options.Separator)
                           : () => new RowByLine(stream, Length);

            return options.ParallelProcessing
                    ? GetParallel()
                    : GetSequential();

            IEnumerable<T> GetSequential()
            {
                var buffer = new string[options.ColumnCount];
                var stringCache = options.StringFactory?.Invoke();
                var getField = new Func<int, string>(i => buffer[i]);

                return GetRecordsSequential(Parser, func, options.HasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, options.Separator, _quote);

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
                            var buf = new string[options.ColumnCount];

                            return new
                            {
                                buffer = buf,
                                lockObj = new object(),
                                stringCache = options.StringFactory?.Invoke(),
                                getField = new Func<int, string>(i => buf[i])
                            };
                        })
                        .ToArray();

                return GetRecordsParallel(Parser, func, options.HasHeader);

                T Parser(ReadOnlyMemory<char> memory, int i)
                {
                    var finder = new TextFindHelper(memory.Span, options.Separator, _quote);

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
    }
}