using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ISpanCSVReader<T>
    {
        T Parse(string str);
    }

    public class SpanCSVReader<T> : ISpanCSVReader<T>
    {
        private readonly Func<string, (int, int)[], T> parser;
        private readonly int[] config;
        private readonly int nth;

        public SpanCSVReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            parser = GenericRecordParser.RecordParserSpan<T>(list).Compile();
        }

        public T Parse(string str)
        {
            var csv = GenericRecordParser.IndexedSplit(str, ";", config, nth, ValueTuple.Create);

            return parser(str, csv);
        }
    }
}
