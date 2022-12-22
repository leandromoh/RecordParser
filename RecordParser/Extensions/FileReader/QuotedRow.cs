using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace RecordParser.Extensions.FileReader
{
    internal class QuotedRow : IFL
    {
        private int i = 0;
        private int j = 0;
        private int c;
        private TextReader reader;
        private bool initial = true;

        private bool yieldLast = false;

        private int bufferLength;
        private char[] buffer;
        public readonly string separator;

        public QuotedRow(TextReader reader, int bufferLength, string separator)
        {
            this.reader = reader;

            buffer = ArrayPool<char>.Shared.Rent(bufferLength);
            this.bufferLength = buffer.Length;

            this.separator = separator;
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

        public IEnumerable<ReadOnlyMemory<char>> TryReadLine()
        {
            int Peek() => i < bufferLength ? buffer[i] : -1;

            var hasBufferToConsume = false;
            var quote = '"';

        reloop:

            j = i;

        outerWhile:

            while (hasBufferToConsume = i < bufferLength)
            {
                c = buffer[i++];

                //  ReadOnlySpan<char> look = buffer.AsSpan().Slice(j, i - j);

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
                            ; // sdfdsf;

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
