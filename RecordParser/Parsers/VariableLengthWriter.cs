using System;

namespace RecordParser.Parsers
{
    public interface IVariableLengthWriter<T>
    {
        bool TryFormat(T instance, Span<char> destination, out int charsWritten);
    }

    internal delegate (bool success, int charsWritten) FuncSpanSpanTInt<T>(Span<char> span, ReadOnlySpan<char> delimiter, T inst);

    internal class VariableLengthWriter<T> : IVariableLengthWriter<T>
    {
        private readonly FuncSpanSpanTInt<T> parse;
        private readonly string separator;

        public VariableLengthWriter(FuncSpanSpanTInt<T> parse, string separator)
        {
            this.parse = parse;
            this.separator = separator;
        }

        public bool TryFormat(T instance, Span<char> destination, out int charsWritten)
        {
            var result = parse(destination, separator, instance);

            charsWritten = result.charsWritten;
            return result.success;
        }
    }
}
