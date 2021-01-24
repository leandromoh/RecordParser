using RecordParser.Generic;
using System;
using System.Buffers;
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
        private readonly Func<string[], T> parser;
        private readonly int[] config;
        private readonly int nth;
        private readonly string delimiter;

        internal CSVReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser, string separator)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
            delimiter = separator;
        }

        public T Parse(string str)
        {
            var span = str.AsSpan();
            Span<(int start, int length)> indices = stackalloc (int, int)[config.Length];
            TextFindHelper.SetStartLengthPositions(span, delimiter, config, nth + 1, in indices);

            var csv = new string[config.Length];
            for (var i = 0; i < config.Length; i++)
                csv[i] = new string(span.Slice(indices[i].start, indices[i].length).Trim());

            T result = parser(csv);
            return result;
        }
    }
}
