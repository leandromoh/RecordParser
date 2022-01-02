using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace RecordParser.Extensions
{
    public static partial class Exasd
    {
        private class RowByLine : IFL
        {
            private int i = 0;
            private int j = 0;
            private int c;
            private TextReader reader;
            private bool initial = true;

            private bool yieldLast = false;

            private int bufferLength;
            private char[] buffer;

            public RowByLine(TextReader reader, int bufferLength)
            {
                this.reader = reader;

                buffer = ArrayPool<char>.Shared.Rent(bufferLength);
                this.bufferLength = buffer.Length;
            }

            public int FillBufferAsync()
            {
                var len = i - j;
                if (initial == false)
                {
                    Array.Copy(buffer, j, buffer, 0, len);
                }

                var totalRead = reader.Read(buffer, len, bufferLength - len);
                bufferLength = len + totalRead;

                i = 0;
                j = 0;

                initial = false;

                if (totalRead == 0 && len != 0 && yieldLast == false)
                {
                    yieldLast = true;
                    return len;
                }

                return totalRead;
            }

            public IEnumerable<Memory<char>> TryReadLine()
            {
                int Peek() => i < bufferLength ? buffer[i] : -1;

                var hasBufferToConsume = false;

            reloop:

                j = i;

                while (hasBufferToConsume = i < bufferLength)
                {
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
                    if (yieldLast)
                        yield return buffer.AsMemory(j, i - j);

                    yield break;
                }

                yield return buffer.AsMemory(j, i - j);
                goto reloop;
            }

            public void Dispose()
            {
                if (buffer != null)
                {
                    ArrayPool<char>.Shared.Return(buffer);
                    buffer = null;
                }
            }
        }
    }
}
