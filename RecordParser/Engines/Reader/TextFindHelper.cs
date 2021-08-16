using System;
using System.Buffers;

namespace RecordParser.Engines.Reader
{
    internal ref struct TextFindHelper
    {
        private readonly ReadOnlySpan<char> source;
        private readonly string delimiter;

        private int scanned;
        private int position;
        private int current;

        private char[] buffer;

        public TextFindHelper(ReadOnlySpan<char> source, string delimiter)
        {
            this.source = source;
            this.delimiter = delimiter;

            scanned = -delimiter.Length;
            position = 0;
            current = 0;
            buffer = null;
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
                buffer = null;
            }
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

            var unlook = source.Slice(scanned);
            var isQuotedField = unlook.TrimStart().StartsWith("\"");

            if (isQuotedField)
            {
                return ParseQuotedChuck();
            }

            position = unlook.IndexOf(delimiter);
            if (position < 0)
            {
                position = source.Length - scanned;
            }

            return source.Slice(scanned, position);
        }

        private ReadOnlySpan<char> ParseQuotedChuck()
        {
            const char singleQuote = '"';
            var unlook = source.Slice(scanned);
            scanned += unlook.IndexOf(singleQuote) + 1;
            unlook = source.Slice(scanned);
            position = 0;

            buffer ??= ArrayPool<char>.Shared.Rent(unlook.Length);
            Span<char> resp = buffer;

            for (int i = 0, j = 0; i < unlook.Length; i++)
            {
                var c = unlook[i];

                switch (c)
                {
                    case '"':
                        var next = unlook.Slice(i + 1);
                        if (next.IsEmpty)
                        {
                            position += i;
                            return resp.Slice(0, j);
                        }
                        if (next[0] == '"')
                        {
                            resp[j++] = '"';
                            i++;
                            continue;
                        }
                        if (next.StartsWith(delimiter))
                        {
                            position += i + 1;
                            return resp.Slice(0, j);
                        }

                        throw new Exception("Corrupt field found. A double quote is not escaped or there is extra data after a quoted field.");

                    default:
                        resp[j++] = c;
                        continue;
                }
            }

            throw new Exception("quoted field missing end quote");
        }
    }
}
