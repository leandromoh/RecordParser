using System;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);
        T Parse(ReadOnlySpan<char> line, Action<Exception, int> exceptionHandler);
    }

    internal class FixedLengthReader<T> : IFixedLengthReader<T>
    {
        private readonly FuncSpanT<T> parser;
        private readonly FuncSpanTSafe<T> parserWithExceptionHandler;

        internal FixedLengthReader(FuncSpanT<T> parser, FuncSpanTSafe<T> parserWithExceptionHandler)
        {
            this.parser = parser;
            this.parserWithExceptionHandler = parserWithExceptionHandler;
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

        public T Parse(ReadOnlySpan<char> line, Action<Exception, int> exceptionHandler)
        {
            return parserWithExceptionHandler(line, exceptionHandler);
        }
    }
}
