using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RecordParser.Extensions
{
    public static partial class Exasd
    {
        // 104
        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        {
            var items = new RowByLine(stream, length);

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
            {
                yield return reader.Parse(x.Span);
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine())
                {
                    yield return reader.Parse(x.Span);
                }
            }
        }

        // 108
        //public static async IAsyncEnumerable<T> GetRecordsAsync<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
        //{
        //    await using var e = new RowByLine(stream).GetRecordsAsync().GetAsyncEnumerator();

        //    if (hasHeader && await e.MoveNextAsync() == false)
        //        yield break;

        //    while (await e.MoveNextAsync())
        //    {
        //        var item = e.Current;

        //        var line = item.buffer.AsMemory().Slice(0, item.length);

        //        var res = reader.Parse(line.Span);

        //        yield return res;
        //    }
        //}

        private class RowByLine : IFL
        {
            private int i = 0;
            private int j = 0;
            private int c;
            private TextReader reader;
            private bool initial = true;

            private int bufferLength;
            private char[] buffer;

            public RowByLine(TextReader reader) : this(reader, (int)Math.Pow(2, 23))
            {

            }

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
                    yield break;
                }

                yield return buffer.AsMemory(j, i - j);
                goto reloop;
            }
        }
    }
}
