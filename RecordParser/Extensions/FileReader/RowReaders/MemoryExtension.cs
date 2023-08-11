using System;
using System.Runtime.CompilerServices;

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal static class MemoryExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<char> TrimEndLineEnd(this Memory<char> current)
        {
            var i = current.Length - 1;

            for (; i >= 0; i--)
            {
                if (current.Span[i] is '\n' or '\r' == false)
                    break;
            }

            return current.Slice(0, i + 1);
        }
    }
}
