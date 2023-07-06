﻿using System;
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
                if (yieldLast)
                    yield return buffer.AsMemory(j, i - j);

                yield break;
            }

            yield return buffer.AsMemory(j, i - j);
            goto reloop;
        }
    }
}
