using RecordParser.Engines.Reader;
using System;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReader<T>
    {
        /// <summary>
        /// Converts a span of characters to object of type <typeparamref name="T"/>.
        /// In case of failure in property mapping, the callback <paramref name="exceptionHandler"/>
        /// is called, then parsing continues for the next property.
        /// </summary>
        /// <param name="line">The span of characters to parse.</param>
        /// <param name="exceptionHandler">
        /// Callback function to be invoked when any property mapping throws an exception.
        /// The first parameter is the thrown exception.
        /// The second parameter is the specified index of the property whose mapping failed.
        /// </param>
        /// <returns>
        /// An object that is equivalent to the values contained in <paramref name="line"/>.
        /// </returns>
        T Parse(ReadOnlySpan<char> line, Action<Exception, int> exceptionHandler);

        /// <summary>
        /// Converts a span of characters to object of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="line">The span of characters to parse.</param>
        /// <returns>
        /// An object that is equivalent to the values contained in <paramref name="line"/>.
        /// </returns>
        T Parse(ReadOnlySpan<char> line);

        /// <summary>
        /// Converts a span of characters to object of type <typeparamref name="T"/>.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <param name="line">The span of characters to parse.</param>
        /// <param name="result">
        /// When this method returns, contains the result of successfully parsing <paramref name="line"/>
        /// or an undefined value on failure.
        /// </param>
        /// <returns>
        /// true if value was successfully parsed; otherwise, false.
        /// </returns>
        /// <remarks>
        /// The parsing is interrupted at the first property mapping failure.
        /// </remarks>
        bool TryParse(ReadOnlySpan<char> line, out T result);

        internal string Separator { get; }
    }

    internal class VariableLengthReader<T> : IVariableLengthReader<T>
    {
        private readonly FuncSpanArrayT<T> parser;
        private readonly FuncSpanArrayTSafe<T> parserWithExceptionHandler;
        private readonly string delimiter;
        private readonly (char ch, string str) quote;

        string IVariableLengthReader<T>.Separator => delimiter;

        internal VariableLengthReader(FuncSpanArrayT<T> parser, FuncSpanArrayTSafe<T> parserWithExceptionHandler, string separator, char quote)
        {
            this.parser = parser;
            this.parserWithExceptionHandler = parserWithExceptionHandler;
            delimiter = separator;
            this.quote = (quote, quote.ToString());
        }

        public T Parse(ReadOnlySpan<char> line, Action<Exception, int> exceptionHandler)
        {
            var finder = new TextFindHelper(line, delimiter, quote);

            try
            {
                return parserWithExceptionHandler(in finder, exceptionHandler);
            }
            finally
            {
                finder.Dispose();
            }
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
