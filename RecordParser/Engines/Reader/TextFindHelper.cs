using System;

namespace RecordParser.Engines.Reader
{
    internal ref struct TextFindHelper
    {
        private readonly ReadOnlySpan<char> source;
        private readonly string delimiter;

        private int scanned;
        private int position;
        private int current;

        public TextFindHelper(ReadOnlySpan<char> source, string delimiter)
        {
            this.source = source;
            this.delimiter = delimiter;

            scanned = -delimiter.Length;
            position = 0;
            current = 0;
        }

        public ReadOnlySpan<char> getValue(int index)
        {
            if (index < current)
                throw new Exception("can only be fowarrd");

            while (current <= index)
            {
                var range = ParseChunk();

                if (index == current++)
                {
                    return range;
                }
            }

            throw new Exception("invalid index for line");
        }

        private ReadOnlySpan<char> ParseChunk()
        {
            scanned += position + delimiter.Length;

            position = source.Slice(scanned).IndexOf(delimiter);
            if (position < 0)
            {
                position = source.Length - scanned;
            }

            return source.Slice(scanned, position);
        }
    }
}
