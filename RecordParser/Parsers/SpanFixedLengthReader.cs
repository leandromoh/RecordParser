﻿using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ISpanFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
    }

    public delegate T FuncSpanArrayT<T>(ReadOnlySpan<char> line, ReadOnlySpan<(int, int)> config);
    public delegate T FuncTSpanArrayT<T>(T instance, ReadOnlySpan<char> line, ReadOnlySpan<(int, int)> config);

    public class SpanFixedLengthReader<T> : ISpanFixedLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly ReadOnlyMemory<(int start, int length)> config;

        internal SpanFixedLengthReader(IEnumerable<MappingConfiguration> list, FuncSpanArrayT<T> parser)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            this.parser = parser;
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            return parser(line, config.Span);
        }
    }
}
