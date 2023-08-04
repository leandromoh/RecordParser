using System;
using System.Runtime.CompilerServices;

namespace RecordParser.Extensions.FileReader.RowReaders
{
    internal static class MemoryExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<char> TrimEnd(this Memory<char> current)
        {
            var i = current.Length - 1;

            for (; i >= 0; i--)
            {
                if (char.IsWhiteSpace(current.Span[i]) == false)
                    break;
            }

            return current.Slice(0, i + 1);
        }
    }
}
