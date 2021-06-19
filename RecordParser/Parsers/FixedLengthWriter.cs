using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthWriter<T>
    {
        bool TryFormat(T instance, Span<char> destination, out int charsWritten);

        [Obsolete("Method was renamed to TryFormat. Parse will eventually be removed in future release.")]
        bool Parse(T instance, Span<char> destination, out int charsWritten);
    }

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
