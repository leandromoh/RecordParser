using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ISpanFixedLengthReader<T>
    {
        T Parse(string line);
    }

    public class SpanFixedLengthReader<T> : ISpanFixedLengthReader<T>
    {
        private readonly Func<string, (int, int)[], T> parser;
        private readonly (int start, int length)[] config;

        internal SpanFixedLengthReader(IEnumerable<MappingConfiguration> list, Func<string, (int, int)[], T> parser)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            this.parser = parser;
        }

        public T Parse(string line)
        {
            return parser(line, config);
        }
    }
}
