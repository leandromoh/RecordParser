﻿using RecordParser.Builders.Reader;
using RecordParser.Engines.Reader;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
    }

    internal class VariableLengthReader<T> : IVariableLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly ReadOnlyMemory<int> config;
        private readonly ReadOnlyMemory<int> stringFields;
        private readonly int maxColumnIndex;
        private readonly string delimiter;

        internal VariableLengthReader(IEnumerable<MappingReadConfiguration> list, FuncSpanArrayT<T> parser, string separator)
        {
            var temp = list.Select(x => x.start).ToArray();

            stringFields = list.Where(x => x.type == typeof(string)).Select(x => x.start).ToArray();
            config = temp;
            maxColumnIndex = temp.Max();
            this.parser = parser;
            delimiter = separator;
        }

#if NET5_0
        [SkipLocalsInit]
#endif
        public T Parse(ReadOnlySpan<char> line)
        {
            Span<(int start, int length)> positions = stackalloc (int, int)[config.Length];
            TextFindHelper.SetStartLengthPositions(line, delimiter, config.Span, maxColumnIndex, in positions);

            var s = stringFields.Span;
            for (var i = 0; i < s.Length; i++)
            {
                var index = s[i];
                var position = positions[index];

                if (position.start != 0 && line[position.start - 1] == '"' && line[position.start + position.length] == '"')
                    positions[index] = (position.start - 1, position.length + 2);
            }

            return parser(line, positions);
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
