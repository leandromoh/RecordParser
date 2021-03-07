using System;

namespace RecordParser.Test
{
    public static class SpanExtensions
    {
        public static string ToUpper(this ReadOnlySpan<char> value)
        {
            Span<char> temp = stackalloc char[value.Length];
            value.ToUpperInvariant(temp);
            return temp.ToString();
        }

        public static string ToLower(this ReadOnlySpan<char> value)
        {
            Span<char> temp = stackalloc char[value.Length];
            value.ToLowerInvariant(temp);
            return temp.ToString();
        }
    }
}
