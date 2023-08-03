using RecordParser.Engines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal partial class RowByQuote : RowBy
    {
        public readonly string separator;

        public RowByQuote(TextReader reader, int bufferLength, string separator)
            : base(reader, bufferLength)
        {
            this.separator = separator;
#if NETCOREAPP3_1_OR_GREATER
            SIMD = AvaibleSIMD();
#endif
        }

#if NETCOREAPP3_1_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
#endif
        public override IEnumerable<ReadOnlyMemory<char>> ReadLines()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;
            var quote = QuoteHelper.Quote.Char;

        reloop:

            j = i;

            while (hasBufferToConsume = i < bufferLength)
            {
#if NETCOREAPP3_1_OR_GREATER
                SIMD?.Invoke();
#endif
                c = buffer[i++];

            charLoaded:

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
                    ReadOnlySpan<char> span = buffer.AsSpan().Slice(0, i - 1);
                    var isQuotedField = span.TrimEnd().EndsWith(separator);

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
        }
    }
}
