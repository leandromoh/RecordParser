using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(string line);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly Func<string[], T> parser;
        private readonly (int start, int length)[] config;

        internal FixedLengthReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
        {
            config = list.Select(x => (x.start, x.length.Value)).ToArray();
            this.parser = parser;
        }

        public T Parse(string line)
        {
            var span = line.AsSpan();
            var csv = new string[config.Length];

            for (var i = 0; i < config.Length; i++)
                csv[i] = new string(span.Slice(config[i].start, config[i].length).Trim());

            return parser(csv);
        }
    }
}
