using System;

namespace RecordParser.Engines.Reader
{
    internal static class TextFindHelper
    {
        public static void SetStartLengthPositions(ReadOnlySpan<char> line, ReadOnlySpan<char> delimiter, ReadOnlySpan<int> config, int maxColumnIndex, in Span<(int start, int length)> csv)
        {
            var scanned = -delimiter.Length;
            var position = 0;

            for (int i = 0, j = 0; i <= maxColumnIndex && j < config.Length; i++)
            {
                var range = ParseChunk(in line, ref scanned, ref position, in delimiter);

                if (i == config[j])
                {
                    csv[j] = range;
                    j++;
                }
            }
        }

        private static (int start, int length) ParseChunk(in ReadOnlySpan<char> line, ref int scanned, ref int position, in ReadOnlySpan<char> delimiter)
        {
            scanned += position + delimiter.Length;

            var unlook = line.Slice(scanned);

            if (unlook.TrimStart().StartsWith("\""))
            {
                return QuoteField.ParseQuotedChuck(line, ref scanned, ref position, delimiter);
            }

            position = unlook.IndexOf(delimiter);
            if (position < 0)
            {
                position = line.Length - scanned;
            }

            return (scanned, position);
        }
    }
}
