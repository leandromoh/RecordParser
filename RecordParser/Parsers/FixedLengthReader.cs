using RecordParser.Generic;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public class FixedLengthReader<T>
    {
        private readonly GenericRecordParser<T> parser;
        private readonly (int start, int length)[] config;

        public FixedLengthReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            parser = new GenericRecordParser<T>(list);
        }

        public T Parse(string line)
        {
            var csv = new string[config.Length];

            for (var i = 0; i < config.Length; i++)
                csv[i] = line.Substring(config[i].start, config[i].length);

            return parser.Parse(csv);
        }
    }
}
