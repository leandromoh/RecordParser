using System;

namespace RecordParser.Engines.Reader
{
    internal static class TextFindHelper
    {
        public static void SetStartLengthPositions(ReadOnlySpan<int> strIndexes, ReadOnlySpan<char> line, ReadOnlySpan<char> delimiter, ReadOnlySpan<int> config, int maxColumnIndex, in Span<(int start, int length)> csv)
        {
            var scanned = -delimiter.Length;
            var position = 0;

            for (int i = 0, j = 0; i <= maxColumnIndex && j < config.Length; i++)
            {
                var isStr = strIndexes.IsEmpty == false && i == strIndexes[0];
                var range = ParseChunk(isStr, in line, ref scanned, ref position, in delimiter);

                if (i == config[j])
                {
                    if (isStr)
                        strIndexes = strIndexes.Slice(1);

                    csv[j] = range;
                    j++;
                }
            }
        }

        private static (int start, int length) ParseChunk(bool isStr, in ReadOnlySpan<char> line, ref int scanned, ref int position, in ReadOnlySpan<char> delimiter)
        {
            scanned += position + delimiter.Length;

            var unlook = line.Slice(scanned);

            if (unlook.TrimStart().StartsWith("\""))
            {
                return QuoteField.ParseQuotedChuck(isStr, line, ref scanned, ref position, delimiter);
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
