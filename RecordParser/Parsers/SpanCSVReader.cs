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
            (int, int)[] csv = IndexOfNth(str, delimiter, config, nth + 1);
            T result = parser(str, csv);
            return result;
        }

        private static (int, int)[] IndexOfNth(ReadOnlySpan<char> span, ReadOnlySpan<char> delimiter, int[] config, int size)
        {
            var csv = new (int, int)[config.Length];
            var scanned = -1;
            var position = 0;
            var j = 0;

            for (var i = 0; i < size && j < config.Length; i++)
            {
                var range = ParseChunk(ref span, ref scanned, ref position, delimiter);

                if (i == config[j])
                {
                    csv[j] = range;
                    j++;
                }
            }

            return csv;
        }

        private static (int, int) ParseChunk(ref ReadOnlySpan<char> span, ref int scanned, ref int position, ReadOnlySpan<char> delimiter)
        {
            scanned += position + 1;

            position = span.Slice(scanned, span.Length - scanned).IndexOf(delimiter);
            if (position < 0)
            {
                position = span.Slice(scanned, span.Length - scanned).Length;
            }

            return (scanned, position);
        }
    }
}
