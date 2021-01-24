using System;

namespace RecordParser.Generic
{
    public static class TextFindHelper
    {
        public static void SetStartLengthPositions(ReadOnlySpan<char> span, ReadOnlySpan<char> delimiter, int[] config, int size, in Span<(int start, int length)> csv)
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

        private static (int start, int length) ParseChunk(in ReadOnlySpan<char> span, ref int scanned, ref int position, in ReadOnlySpan<char> delimiter)
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
