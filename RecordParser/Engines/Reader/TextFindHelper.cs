using System;
using System.Buffers;

namespace RecordParser.Engines.Reader
{
    internal ref struct TextFindHelper
    {
        private readonly ReadOnlySpan<char> line;
        private readonly string delimiter;
        private readonly (char ch, string str) quote;

        private int scanned;
        private int position;
        private int currentIndex;
        private ReadOnlySpan<char> currentValue;

        private char[] buffer;

        public TextFindHelper(ReadOnlySpan<char> source, string delimiter, (char ch, string str) quote)
        {
            this.line = source;
            this.delimiter = delimiter;
            this.quote = quote;

            scanned = -delimiter.Length;
            position = 0;
            currentIndex = -1;
            currentValue = default;
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

        public ReadOnlySpan<char> GetValue(int index)
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
                currentValue = ParseChunk(match);

                if (match)
                {
                    return currentValue;
                }
            }

            throw new Exception("invalid index for line");
        }

        private ReadOnlySpan<char> ParseChunk(bool match)
        {
            scanned += position + delimiter.Length;

            var unlook = line.Slice(scanned);
            var isQuotedField = unlook.TrimStart().StartsWith(quote.str);

            if (isQuotedField)
            {
                return ParseQuotedChuck(unlook);
            }

            position = unlook.IndexOf(delimiter);
            if (position < 0)
            {
                position = line.Length - scanned;
            }

            return line.Slice(scanned, position);
        }

        private ReadOnlySpan<char> ParseQuotedChuck(ReadOnlySpan<char> unlook)
        {
            const string corruptFieldError = "Double quote is not escaped or there is extra data after a quoted field.";

            position = unlook.IndexOf(quote.ch) + 1; // +1 for quote
            unlook = unlook.Slice(position);

            //if (match)
            //{
            buffer ??= ArrayPool<char>.Shared.Rent(unlook.Length);
            Span<char> resp = buffer;
            var x = -1;
            var state = 2;

            // 1 Outside quoted field
            // 2 Inside quoted field
            // 3 Possible escaped quote (the first " in "")

            for (int i = 0; i < unlook.Length; i++)
            {
                var c = unlook[i];

                switch (state)
                {
                    case 2:
                        if (c == quote.ch)
                        {
                            state = 3;

                            // is this quote the last character of the record?
                            // if so, it is the closing quote of the last field
                            if (i + 1 == unlook.Length)
                            {
                                c = default;
                                x++;
                                i++;
                                goto case 3;
                            }
                        }
                        resp[++x] = c;

                        continue;
                    case 3:
                        if (c == quote.ch)
                        {
                            state = 2;
                            continue;
                        }
                        else
                        {
                            state = 1;

                            var value = resp.Slice(0, x);
                            position += i;
                            var next = unlook.Slice(i);

                            if (next.IsWhiteSpace())
                            {
                                return value;
                            }

                            for (var t = 0; t < next.Length; t++)
                                if (next.Slice(t).StartsWith(delimiter))
                                {
                                    position += t;
                                    return value;
                                }
                                else if (char.IsWhiteSpace(next[t]) is false)
                                {
                                    break;
                                }

                            throw new Exception(corruptFieldError);
                        }
                }

                //if (c == quote.ch)
                //{
                //    var next = unlook.Slice(i + 1);
                //    if (next.TrimStart().IsEmpty)
                //    {
                //        position += i;
                //        return resp.Slice(0, j);
                //    }
                //    if (next[0] == quote.ch)
                //    {
                //        resp[j++] = quote.ch;
                //        i++;
                //        continue;
                //    }

                //    for (var t = 0; t < next.Length; t++)
                //        if (next.Slice(t).StartsWith(delimiter))
                //        {
                //            position += i + 1 + t;
                //            return resp.Slice(0, j);
                //        }
                //        else if (char.IsWhiteSpace(next[t]) is false)
                //        {
                //            break;
                //        }

                //    throw new Exception(corruptFieldError);

                //}
                //else
                //{
                //    resp[j++] = c;
                //    continue;
                //}
            }

            //}
            //else
            //{
            //    for (int i = 0; i < unlook.Length; i++)
            //    {
            //        if (unlook[i] == quote.ch)
            //        {
            //            var next = unlook.Slice(i + 1);
            //            if (next.TrimStart().IsEmpty)
            //            {
            //                position += i;
            //                return default;
            //            }
            //            if (next[0] == quote.ch)
            //            {
            //                i++;
            //                continue;
            //            }

            //            for (var t = 0; t < next.Length; t++)
            //                if (next.Slice(t).StartsWith(delimiter))
            //                {
            //                    position += i + 1 + t;
            //                    return default;
            //                }
            //                else if (char.IsWhiteSpace(next[t]) is false)
            //                {
            //                    break;
            //                }

            //            throw new Exception(corruptFieldError);

            //        }
            //        else
            //        {
            //            continue;
            //        }
            //    }
            //}

            throw new Exception("Quoted field is missing closing quote.");
        }
    }
}
