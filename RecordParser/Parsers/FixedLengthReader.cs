using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly FuncSpanT<T> parser;

        internal FixedLengthReader(FuncSpanT<T> parser)
        {
            this.parser = parser;
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            return parser(line);
        }

        public bool TryParse(ReadOnlySpan<char> line, out T result)
        {
            try
            {
                result = Parse(line);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}
