using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ISpanFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
    }

    public class SpanFixedLengthReader<T> : ISpanFixedLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly (int start, int length)[] config;

        internal SpanFixedLengthReader(IEnumerable<MappingConfiguration> list, FuncSpanArrayT<T> parser)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            this.parser = parser;
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            return parser(line, config);
        }
    }
}
