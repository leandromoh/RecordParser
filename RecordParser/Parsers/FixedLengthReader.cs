﻿using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly ReadOnlyMemory<(int start, int length)> config;

        internal FixedLengthReader(IEnumerable<MappingConfiguration> list, FuncSpanArrayT<T> parser)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            this.parser = parser;
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            return parser(line, config.Span);
        }

        public bool TryParse(ReadOnlySpan<char> line, out T result)
        {
            try
            {
                result = Parse(line);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
