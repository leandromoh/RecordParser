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
    }
}
