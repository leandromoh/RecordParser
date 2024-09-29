#if NETSTANDARD2_0 || NETFRAMEWORK

using System;

namespace RecordParser.Test
{
    internal static class Format
    {
        public static bool TryFormat(this int @this, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null)
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

        public static bool TryFormat(this long @this, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null)
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

        public static bool TryFormat(this DateTime @this, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider provider = null)
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
