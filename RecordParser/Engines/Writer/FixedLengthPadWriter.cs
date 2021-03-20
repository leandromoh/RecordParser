using System;

namespace RecordParser.Engines.Writer
{
    internal class FixedLengthPadWriter
    {
        public static Span<char> PadRight(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total);

            for (int i = span.Length, totalWidth = total.Length; i < totalWidth; i++)
                total[i] = paddingChar;

            return total;
        }

        public static Span<char> PadLeft(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total.Slice(diff));

            for (var i = 0; i < diff; i++)
                total[i] = paddingChar;

            return total;
        }
    }
}
