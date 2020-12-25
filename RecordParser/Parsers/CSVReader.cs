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

        internal CSVReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
            delimiter = ";";
        }

        public T Parse(string str)
        {
            Span<int> positions = stackalloc int[nth + 2];

            var i = 0;
            foreach (var x in GenericRecordParser.IndexOfNth(str, delimiter, nth + 2))
            {
                positions[i++] = x;
            }

            if (i < positions.Length)
                throw new InvalidOperationException("menos colunas do q devia");

            i = 0;
            var csv = new string[config.Length];
            
            foreach (var index in config)
            {
                var start = positions[index];
                var length = positions[index + 1] - start - delimiter.Length;
                csv[i++] = str.Substring(start, length).Trim();
            }

            var result = parser(csv);
            return result;
        }
    }
}
