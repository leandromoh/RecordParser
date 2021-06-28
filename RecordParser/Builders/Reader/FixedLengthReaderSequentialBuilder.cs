using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IFixedLengthReaderSequentialBuilder<T>
    {
        /// <summary>
        /// Creates the reader object using the registered mappings.
        /// </summary>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <param name="factory">Function that generates an instance of <typeparamref name="T"/>.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The reader object.</returns>
        IFixedLengthReader<T> Build(CultureInfo cultureInfo = null, Func<T> factory = null);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);

        /// <summary>
        /// Advance the current position by the number specified in <paramref name="length"/>.
        /// The skipped positions will be ignored and not mapped.
        /// </summary>
        /// <param name="length">The number of positions to advance.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthReaderSequentialBuilder<T> Skip(int length);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, FuncSpanT<R> converter = null);
    }

    public class FixedLengthReaderSequentialBuilder<T> : IFixedLengthReaderSequentialBuilder<T>
    {
        private readonly IFixedLengthReaderBuilder<T> indexed = new FixedLengthReaderBuilder<T>();
        private int currentPosition = 0;

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int length,
            FuncSpanT<R> converter = null)
        {
            indexed.Map(ex, currentPosition, length, converter);
            currentPosition += length;
            return this;
        }

        /// <summary>
        /// Advance the current position by the number specified in <paramref name="length"/>.
        /// The skipped positions will be ignored and not mapped.
        /// </summary>
        /// <param name="length">The number of positions to advance.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderSequentialBuilder<T> Skip(int length)
        {
            currentPosition += length;
            return this;
        }

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        /// <summary>
        /// Creates the reader object using the registered mappings.
        /// </summary>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <param name="factory">Function that generates an instance of <typeparamref name="T"/>.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The reader object.</returns>
        public IFixedLengthReader<T> Build(CultureInfo cultureInfo = null, Func<T> factory = null) 
            => indexed.Build(cultureInfo, factory);
    }
}
