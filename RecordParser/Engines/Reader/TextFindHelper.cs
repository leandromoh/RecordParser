using System;
using System.Buffers;

namespace RecordParser.Engines.Reader
{
    internal ref struct TextFindHelper
    {
        private readonly ReadOnlySpan<char> line;
        private ReadOnlySpan<char> currentValue;
        private TextFindHelperCore core;

        public TextFindHelper(ReadOnlySpan<char> source, string delimiter, (char ch, string str) quote)
        {
            line = source;
            currentValue = default;
            core = new TextFindHelperCore(delimiter, quote);
        }

        public ReadOnlySpan<char> GetValue(int index)
        {
            currentValue = core.GetValue(index, currentValue, line);

            return currentValue;
        }

        public void Dispose() => core.Dispose();
    }

    internal struct TextFindHelperCore
    {
        private readonly string delimiter;
        private readonly (char ch, string str) quote;

        private int scanned;
        private int position;
        private int currentIndex;

        private char[] buffer;

        public TextFindHelperCore(string delimiter, (char ch, string str) quote)
        {
            this.delimiter = delimiter;
            this.quote = quote;

            scanned = -delimiter.Length;
            position = 0;
            currentIndex = -1;
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

        public ReadOnlySpan<char> GetValue(int index, ReadOnlySpan<char> currentValue, ReadOnlySpan<char> line)
        {
            if (index <= currentIndex)
            {
                if (index == currentIndex)
                    return currentValue;
                else
                    throw new Exception("can only be forward");
            }

            while (currentIndex <= index)
            {
                var match = index == ++currentIndex;
                currentValue = ParseChunk(match, line);

                if (match)
                {
                    return currentValue;
                }
            }

            throw new Exception("invalid index for line");
        }

        private ReadOnlySpan<char> ParseChunk(bool match, ReadOnlySpan<char> line)
        {
            scanned += position + delimiter.Length;

            var unlook = line.Slice(scanned);
            var isQuotedField = unlook.TrimStart().StartsWith(quote.str);

            if (isQuotedField)
            {
                return ParseQuotedChuck(match, line);
            }

            position = unlook.IndexOf(delimiter);
            if (position < 0)
            {
                position = line.Length - scanned;
            }

            return line.Slice(scanned, position);
        }

        private ReadOnlySpan<char> ParseQuotedChuck(bool match, ReadOnlySpan<char> line)
        {
            const string corruptFieldError = "Double quote is not escaped or there is extra data after a quoted field.";

            var unlook = line.Slice(scanned);
            scanned += unlook.IndexOf(quote.ch) + 1;
            unlook = line.Slice(scanned);
            position = 0;

            if (match)
            {
                buffer ??= ArrayPool<char>.Shared.Rent(unlook.Length);
                Span<char> resp = buffer;

                for (int i = 0, j = 0; i < unlook.Length; i++)
                {
                    var c = unlook[i];

                    if (c == quote.ch)
                    {
                        var next = unlook.Slice(i + 1);
                        if (next.TrimStart().IsEmpty)
                        {
                            position += i;
                            return resp.Slice(0, j);
                        }
                        if (next[0] == quote.ch)
                        {
                            resp[j++] = quote.ch;
                            i++;
                            continue;
                        }

                        for (var t = 0; t < next.Length; t++)
                            if (next.Slice(t).StartsWith(delimiter))
                            {
                                position += i + 1 + t;
                                return resp.Slice(0, j);
                            }
                            else if (char.IsWhiteSpace(next[t]) is false)
                            {
                                break;
                            }

                        throw new Exception(corruptFieldError);

                    }
                    else
                    {
                        resp[j++] = c;
                        continue;
                    }
                }
            }
            else
            {
                for (int i = 0; i < unlook.Length; i++)
                {
                    if (unlook[i] == quote.ch)
                    {
                        var next = unlook.Slice(i + 1);
                        if (next.TrimStart().IsEmpty)
                        {
                            position += i;
                            return default;
                        }
                        if (next[0] == quote.ch)
                        {
                            i++;
                            continue;
                        }

                        for (var t = 0; t < next.Length; t++)
                            if (next.Slice(t).StartsWith(delimiter))
                            {
                                position += i + 1 + t;
                                return default;
                            }
                            else if (char.IsWhiteSpace(next[t]) is false)
                            {
                                break;
                            }

                        throw new Exception(corruptFieldError);

                    }
                    else
                    {
                        continue;
                    }
                }
            }

            throw new Exception("Quoted field is missing closing quote.");
        }
    }
}
