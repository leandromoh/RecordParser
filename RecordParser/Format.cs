#if NETSTANDARD2_0

using System;

namespace RecordParser
{
    internal static class Format
    {
        public static bool TryFormat<T>(this T @this, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null) where T : IFormattable
        {
            string value = @this.ToString(format.ToString(), provider);
            if (value.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = value.Length;
            value.AsSpan().CopyTo(destination);
            return true;
        }
    }
}

#endif
