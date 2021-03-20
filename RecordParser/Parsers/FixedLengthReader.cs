using RecordParser.Builders.Reader;
using System;
using System.Collections.Generic;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;

        internal FixedLengthReader(IEnumerable<MappingReadConfiguration> list, FuncSpanArrayT<T> parser)
        {
            this.parser = parser;
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            return parser(line, default);
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
