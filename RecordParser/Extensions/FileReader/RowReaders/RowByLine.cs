using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal partial class RowByLine : RowBy
    {
        public RowByLine(TextReader reader, int bufferLength)
            : base(reader, bufferLength)
        {
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

        reloop:

            j = i;

            while (hasBufferToConsume = i < bufferLength)
            {
#if NETCOREAPP3_1_OR_GREATER
                SIMD?.Invoke();
#endif
                c = buffer[i++];

                // '\r' => 13
                // '\n' => 10
                if (c > 13)
                    continue;

                switch (c)
                {
                    case '\r':
                        if (Peek() == '\n')
                        {
                            i++;
                        }
                        goto afterLoop;

                    case '\n':
                        goto afterLoop;
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
