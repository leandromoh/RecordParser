using System;
using System.Collections.Generic;

namespace RecordParser.Extensions.FileReader
{
    internal interface IFL : IDisposable
    {
        int FillBufferAsync();
        IEnumerable<ReadOnlyMemory<char>> TryReadLine();
    }
}
