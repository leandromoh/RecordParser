using RecordParser.Generic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RecordParser.Parsers
{
    public interface ISpanVariableLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
    }

    internal class SpanVariableLengthReader<T> : ISpanVariableLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly int[] config;
        private readonly int nth;
        private readonly string delimiter;

        internal SpanVariableLengthReader(IEnumerable<MappingConfiguration> list, FuncSpanArrayT<T> parser, string separator)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
            delimiter = separator;
        }

#if NET5_0
        [SkipLocalsInit]
#endif
        public T Parse(ReadOnlySpan<char> line)
        {
            Span<(int start, int length)> csv = stackalloc (int, int)[config.Length];
            TextFindHelper.SetStartLengthPositions(line, delimiter, config, nth + 1, in csv);
            T result = parser(line, csv);
            return result;
        }
    }
}
