﻿using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthWriter<T>
    {
        bool TryFormat(T instance, Span<char> destination, out int charsWritten);

        [Obsolete("Method was renamed to TryFormat. Parse will eventually be removed in future release.")]
        bool Parse(T instance, Span<char> destination, out int charsWritten);
    }

    /// <summary>
    /// Tries to format the value of <paramref name="inst"/> into the span of characters <paramref name="span"/>.
    /// </summary>
    /// <param name="span">When this method returns, the <paramref name="inst"/> value formatted as a span of characters.</param>
    /// <param name="inst">The instance to be formatted.</param>
    /// <returns>
    /// For success tuple element: true if the formatting was successful; otherwise, false.
    /// For charsWritten tuple element: When this method returns, the number of characters that were written in <paramref name="span"/>.
    /// </returns>
    public delegate (bool success, int charsWritten) FuncSpanTIntBool<T>(Span<char> span, T inst);

    internal class FixedLengthWriter<T> : IFixedLengthWriter<T>
    {
        private readonly FuncSpanTIntBool<T> parse;

        public FixedLengthWriter(FuncSpanTIntBool<T> parse)
        {
            this.parse = parse;
        }

        [Obsolete("Method was renamed to TryFormat. Parse will eventually be removed in future release.")]
        public bool Parse(T instance, Span<char> destination, out int charsWritten) =>
            TryFormat(instance, destination, out charsWritten);

        public bool TryFormat(T instance, Span<char> destination, out int charsWritten)
        {
            var result = parse(destination, instance);

            charsWritten = result.charsWritten;
            return result.success;
        }
    }
}
