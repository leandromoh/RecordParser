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
            Span<(int, int)> csv = stackalloc (int, int)[config.Length];
            IndexOfNth(str, delimiter, config, nth + 1, in csv);
            T result = parser(str, csv);
            return result;
        }

        private static void IndexOfNth(ReadOnlySpan<char> span, ReadOnlySpan<char> delimiter, int[] config, int size, in Span<(int, int)> csv)
        {
            var scanned = - delimiter.Length;
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
