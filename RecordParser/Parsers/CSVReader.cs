using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ICSVReader<T>
    {
        T Parse(string str);
    }

    public class CSVReader<T> : ICSVReader<T>
    {
        public readonly Func<string[], T> parser;
        private readonly int[] config;
        private readonly int nth;

        public CSVReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            parser = GenericRecordParser.RecordParser<T>(list).Compile();
        }

        public T Parse(string str)
        {
            var csv = GenericRecordParser.IndexedSplit(str, ";", config, nth, 
                (start, length) => str.Substring(start, length).Trim());

            return parser(csv);
        }
    }
}
