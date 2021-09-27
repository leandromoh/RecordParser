using RecordParser.Parsers;
using System;

namespace RecordParser.Test
{
    public static class StringExtensions
    {
        public static readonly FuncSpanTIntBool<string> ToUpperInvariant = (Span<char> span, string text) =>
            (text.AsSpan().ToUpperInvariant(span) is var written && written == text.Length, Math.Max(0, written));

        public static readonly FuncSpanTIntBool<string> ToLowerInvariant = (Span<char> span, string text) =>
            (text.AsSpan().ToLowerInvariant(span) is var written && written == text.Length, Math.Max(0, written));

        public static string Quote(this string value, string separator, bool trim = true)
        {
            if (trim)
                value = value?.Trim();

            if (value == null)
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) > -1 || value.Contains(separator))
            {
                return '"' + value.Replace("\"", "\"\"") + '"';
            }
            else
            {
                return value;
            }
        }
    }
}
