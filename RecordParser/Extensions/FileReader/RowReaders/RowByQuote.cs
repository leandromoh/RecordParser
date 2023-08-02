using RecordParser.Engines;
using System;
using System.Collections.Generic;
using System.IO;

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal class RowByQuote : RowBy
    {
        public readonly string separator;

        public RowByQuote(TextReader reader, int bufferLength, string separator)
            : base(reader, bufferLength)
        {
            this.separator = separator;
        }

        public override IEnumerable<ReadOnlyMemory<char>> ReadLines()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;
            var quote = QuoteHelper.Quote.Char;

        reloop:

            j = i;

        outerWhile:

            while (hasBufferToConsume = i < bufferLength)
            {
                c = buffer[i++];

                // '\r' => 13
                // '\n' => 10
                // '"'  => 34
                if (c > 34)
                    continue;

                if (c == '\r')
                {
                    if (Peek() == '\n')
                    {
                        i++;
                    }
                    goto afterLoop;
                }
                else if (c == '\n')
                {
                    goto afterLoop;
                }
                else if (c == quote)
                {
                    ReadOnlySpan<char> span = buffer.AsSpan();
                    var isQuotedField = span.Slice(0, i - 1).TrimEnd().EndsWith(separator);

                    if (isQuotedField is false)
                        continue;

                    ReadOnlySpan<char> unlook = buffer.AsSpan().Slice(i);

                    for (int z = 0; hasBufferToConsume = z < unlook.Length; z++)
                    {
                        if (unlook[z] != quote)
                            continue;

                        var next = unlook.Slice(z + 1);

                        if (next.IsEmpty)
                            ; 

                        if (next[0] == quote)
                        {
                            z++;
                            continue;
                        }

                        for (var t = 0; t < next.Length; t++)
                            if (next.Slice(t).StartsWith(separator))
                            {
                                i += z + 1 + t;
                                goto outerWhile;
                            }
                            else if (char.IsWhiteSpace(next[t]) is false)
                            {
                                break;
                            }

                        throw new Exception("corruptFieldError");
                    }
                }
            }

        afterLoop:

            if (hasBufferToConsume == false)
            {
                if (yieldLast)
                    yield return buffer.AsMemory(j, i - j).TrimEnd();

                yield break;
            }

            yield return buffer.AsMemory(j, i - j).TrimEnd();
            goto reloop;
        }
    }
}
