using RecordParser.Visitors;
using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        T Parse(ReadOnlyMemory<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly FuncSpanT<T> parser;
        private readonly Func<Foo, T> parser2;

        internal FixedLengthReader(FuncSpanT<T> parser, Func<Foo, T> parser2)
        {
            this.parser = parser;
            this.parser2 = parser2;
        }

        public T Parse(ReadOnlyMemory<char> line)
        {
            return parser2(new Foo(line));
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
