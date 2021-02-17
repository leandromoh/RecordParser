using System;

namespace RecordParser.Parsers
{
    public interface IVariableLengthWriter<T>
    {
        int Parse(T instance, Span<char> destination);
    }

    internal delegate int FuncSpanSpanTInt<T>(Span<char> span, ReadOnlySpan<char> delimiter, T inst);

    internal class VariableLengthWriter<T> : IVariableLengthWriter<T>
    {
        private readonly FuncSpanSpanTInt<T> parse;
        private readonly string separator;

        public VariableLengthWriter(FuncSpanSpanTInt<T> parse, string separator)
        {
            this.parse = parse;
            this.separator = separator;
        }

        public int Parse(T instance, Span<char> destination)
        {
            var charsWritten = parse(destination, separator, instance);
            return charsWritten;
        }
    }
}
