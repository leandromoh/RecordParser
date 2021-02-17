using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthWriter<T>
    {
        int Parse(T instance, Span<char> destination);
    }

    internal delegate int FuncSpanTInt<T>(Span<char> span, T inst);

    internal class FixedLengthWriter<T> : IFixedLengthWriter<T>
    {
        private readonly FuncSpanTInt<T> parse;

        public FixedLengthWriter(FuncSpanTInt<T> parse)
        {
            this.parse = parse;
        }

        public int Parse(T instance, Span<char> destination)
        {
            var charsWritten = parse(destination, instance);
            return charsWritten;
        }
    }
}
