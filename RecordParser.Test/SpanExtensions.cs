using FluentAssertions;
using FluentAssertions.Primitives;
using RecordParser.Parsers;
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

        // FluentAssertions does not support Span yet
        public static StringAssertions Should(this Span<char> value) =>
            value.ToString().Should();

        // FluentAssertions does not support ReadOnlySpan yet
        public static StringAssertions Should(this ReadOnlySpan<char> value) =>
            value.ToString().Should();

        public static AndConstraint<StringAssertions> Be(this StringAssertions value, ReadOnlySpan<char> expected) =>
            value.Be(expected.ToString());

        public static readonly FuncSpanTIntBool ToUpperInvariant = (Span<char> span, ReadOnlySpan<char> text) =>
            (text.ToUpperInvariant(span) is var written && written == text.Length, Math.Max(0, written));

        public static readonly FuncSpanTIntBool ToLowerInvariant = (Span<char> span, ReadOnlySpan<char> text) =>
            (text.ToLowerInvariant(span) is var written && written == text.Length, Math.Max(0, written));
    }
}
