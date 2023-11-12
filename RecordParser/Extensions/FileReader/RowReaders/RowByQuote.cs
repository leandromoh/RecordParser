using RecordParser.Engines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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

            while (hasBufferToConsume = i < bufferLength)
            {
                var index = memory.Span.Slice(i).IndexOfAny('\r', '\n', '"');
                if (index < 0)
                {
                    i = bufferLength;
                    continue;
                }
                else
                {
                    i += index;
                }

                c = buffer[i++];

            charLoaded:

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
                    ReadOnlySpan<char> span = buffer.AsSpan().Slice(0, i - 1);
                    var isQuotedField = IsFirstColumn(span) || span.TrimEnd().EndsWith(separator);

                    if (isQuotedField is false)
                        continue;

                    // 1 Outside quoted field
                    // 2 Inside quoted field
                    // 3 Possible escaped quote (the first " in "")

                    var state = 2;

                    while (hasBufferToConsume = i < bufferLength)
                    {
                        c = buffer[i++];
                        switch (state)
                        {
                            case 2:
                                if (c == quote)
                                    state = 3;
                                continue;
                            case 3:
                                state = c == quote ? 2 : 1;
                                if (state == 1)
                                    goto charLoaded;
                                continue;
                        }
                    }
                }
            }

        afterLoop:

            if (hasBufferToConsume == false)
            {
                if (yieldLast && TryGetRecord(out var x))
                    yield return x;

                yield break;
            }

            if (TryGetRecord(out var y))
                yield return y;

            goto reloop;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool IsFirstColumn(ReadOnlySpan<char> span)
            {
                var onlyWhiteSpace = true;

                for (var i = span.Length - 1; i >= 0; i--)
                {
                    if (char.IsWhiteSpace(span[i]))
                    {
                        if (span[i] is '\n' or '\r')
                            return true;
                        else
                            continue;
                    }
                    onlyWhiteSpace = false;
                    break;
                }

                return onlyWhiteSpace;
            }
        }
    }
}
