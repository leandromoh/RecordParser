using RecordParser.Generic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ISpanCSVReader<T>
    {
        T Parse(ReadOnlySpan<char> str);
    }

    public class SpanCSVReader<T> : ISpanCSVReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly int[] config;
        private readonly int nth;
        private readonly string delimiter;

        internal SpanCSVReader(IEnumerable<MappingConfiguration> list, FuncSpanArrayT<T> parser)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
            delimiter = ";";
        }

        public T Parse(ReadOnlySpan<char> str)
        {
            var csv = GenericRecordParser.IndexOfNth(str, delimiter, config, nth + 2);
            var result = parser(str, csv);
            return result;
        }
    }
}
