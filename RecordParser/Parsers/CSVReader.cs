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
            Span<(int, int)> indices = stackalloc (int, int)[config.Length];
            IndexOfNth(span, delimiter, config, nth + 1, in indices);
            var csv = new string[config.Length];
            for (var i = 0; i < config.Length; i++)
            {
                csv[i] = new string(span.Slice(indices[i].Item1, indices[i].Item2).Trim());
            }

            T result = parser(csv);
            return result;
        }

        private static void IndexOfNth(ReadOnlySpan<char> span, ReadOnlySpan<char> delimiter, int[] config, int size, in Span<(int, int)> csv)
        {
            var scanned = -delimiter.Length;
            var position = 0;

            for (int i = 0, j = 0; i < size && j < config.Length; i++)
            {
                var range = ParseChunk(in span, ref scanned, ref position, in delimiter);

                if (i == config[j])
                {
                    csv[j] = range;
                    j++;
                }
            }
        }

        private static (int, int) ParseChunk(in ReadOnlySpan<char> span, ref int scanned, ref int position, in ReadOnlySpan<char> delimiter)
        {
            scanned += position + delimiter.Length;

            position = span.Slice(scanned, span.Length - scanned).IndexOf(delimiter);
            if (position < 0)
            {
                position = span.Slice(scanned, span.Length - scanned).Length;
            }

            return (scanned, position);
        }
    }
}
