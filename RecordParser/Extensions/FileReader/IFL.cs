using System;
using System.Collections.Generic;

namespace RecordParser.Extensions.FileReader
{
    internal interface IFL : IDisposable
    {
        int FillBuffer();
        IEnumerable<ReadOnlyMemory<char>> ReadLines();
    }
}
