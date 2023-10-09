using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IVariableLengthReaderSequentialBuilder<T>
    {
        /// <summary>
        /// Creates the reader object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits columns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <param name="factory">Function that generates an instance of <typeparamref name="T"/>.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The reader object.</returns>
        IVariableLengthReader<T> Build(string separator, CultureInfo cultureInfo = null, Func<T> factory = null);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);


        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, FuncSpanT<R> converter = null);

        /// <summary>
        /// Advance the current column by the number specified in <paramref name="columnCount"/>.
        /// The skipped columns will be ignored and not mapped.
        /// </summary>
        /// <param name="columnCount">The number of columns to skip.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthReaderSequentialBuilder<T> Skip(int columnCount);
    }

    public class VariableLengthReaderSequentialBuilder<T> : IVariableLengthReaderSequentialBuilder<T>
    {
        private readonly IVariableLengthReaderBuilder<T> indexed = new VariableLengthReaderBuilder<T>();
        private int currentIndex = -1;

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex,
            FuncSpanT<R> converter = null)
        {
            indexed.Map(ex, ++currentIndex, converter);
            return this;
        }

        /// <summary>
        /// Advance the current column by the number specified in <paramref name="columnCount"/>.
        /// The skipped columns will be ignored and not mapped.
        /// </summary>
        /// <param name="columnCount">The number of columns to skip.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthReaderSequentialBuilder<T> Skip(int columnCount)
        {
            currentIndex += columnCount;
            return this;
        }

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        /// <summary>
        /// Creates the reader object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits columns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <param name="factory">Function that generates an instance of <typeparamref name="T"/>.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The reader object.</returns>
        public IVariableLengthReader<T> Build(string separator, CultureInfo cultureInfo = null, Func<T> factory = null) 
            => indexed.Build(separator, cultureInfo, factory);
    }
}
