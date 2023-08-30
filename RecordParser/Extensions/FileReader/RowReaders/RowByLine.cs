using System;
using System.Collections.Generic;
using System.IO;

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal class RowByLine : RowBy
    {
        public RowByLine(TextReader reader, int bufferLength)
            : base(reader, bufferLength)
        {
        }

        public override IEnumerable<ReadOnlyMemory<char>> ReadLines()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;

        reloop:

            j = i;

            while (hasBufferToConsume = i < bufferLength)
            {
                var index = memory.Span.Slice(i).IndexOfAny('\r', '\n');
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
