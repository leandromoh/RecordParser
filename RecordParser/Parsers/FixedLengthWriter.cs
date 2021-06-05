using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthWriter<T>
    {
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

        public bool Parse(T instance, Span<char> destination, out int charsWritten)
        {
            var result = parse(destination, instance);

            charsWritten = result.charsWritten;
            return result.success;
        }
    }
}
