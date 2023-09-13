using RecordParser.Engines.Reader;
using System;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReader<T>
    {
        T Parse(ReadOnlySpan<char> line);
        bool TryParse(ReadOnlySpan<char> line, out T result);

        internal string Separator { get; }
    }

    internal class VariableLengthReader<T> : IVariableLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly string delimiter;
        private readonly (char ch, string str) quote;

        string IVariableLengthReader<T>.Separator => delimiter;

        internal VariableLengthReader(FuncSpanArrayT<T> parser, string separator, char quote)
        {
            this.parser = parser;
            delimiter = separator;
            this.quote = (quote, quote.ToString());
        }

        public T Parse(ReadOnlySpan<char> line)
        {
            var finder = new TextFindHelper(line, delimiter, quote);

            try
            {
                return parser(in finder);
            }
            finally
            {
                finder.Dispose();
            }
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
