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

        public SpanFixedLengthReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            parser = GenericRecordParser.RecordParserSpan<T>(list).Compile();
        }

        public T Parse(string line)
        {
            return parser(line, config);
        }
    }
}
