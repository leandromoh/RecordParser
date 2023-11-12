using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace RecordParser.Extensions.FileReader.RowReaders;

internal abstract class RowBy : IFL
{
    protected int i = 0;
    protected int j = 0;
    protected int c;
    protected TextReader reader;
    protected bool initial = true;

    protected bool yieldLast = false;

    protected int bufferLength;
    protected char[] buffer;
    protected Memory<char> memory;

    public RowBy(TextReader reader, int bufferLength)
    {
        this.reader = reader;

        buffer = ArrayPool<char>.Shared.Rent(bufferLength);
        this.bufferLength = buffer.Length;
    }

    public int FillBuffer()
    {
        var len = i - j;
        if (initial == false)
        {
            if (len == buffer.Length)
                throw new RecordTooLargeException("Record is too large.");

            Array.Copy(buffer, j, buffer, 0, len);
        }

        var totalRead = reader.Read(buffer, len, bufferLength - len);
        bufferLength = len + totalRead;

        memory = buffer.AsMemory(0, bufferLength);
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

    public abstract IEnumerable<ReadOnlyMemory<char>> ReadLines();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool TryGetRecord(out Memory<char> record)
    {
        record = buffer.AsMemory(j, i - j).TrimEndLineEnd();
        return record.Length > 0;
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
