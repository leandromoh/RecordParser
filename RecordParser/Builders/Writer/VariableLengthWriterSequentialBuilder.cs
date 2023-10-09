using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public interface IVariableLengthWriterSequentialBuilder<T>
    {
        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits columns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The writer object.</returns>
        IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        /// <summary>
        /// Advance the current column by the number specified in <paramref name="columnCount"/>.
        /// The skipped columns will be ignored and not mapped.
        /// </summary>
        /// <param name="columnCount">The number of columns to skip.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterSequentialBuilder<T> Skip(int columnCount);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, string format);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="converter">
        /// Custom function to write the member value into span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, FuncSpanTIntBool<R> converter = null);

        IVariableLengthWriterSequentialBuilder<T> Map(Expression<Func<T, string>> ex, FuncSpanTIntBool converter = null);
    }

    public class VariableLengthWriterSequentialBuilder<T> : IVariableLengthWriterSequentialBuilder<T>
    {
        private readonly IVariableLengthWriterBuilder<T> indexed = new VariableLengthWriterBuilder<T>();
        private int currentIndex = -1;

        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits columns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The writer object.</returns>
        public IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null)
            => indexed.Build(separator, cultureInfo);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthWriterSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, string format)
        {
            indexed.Map(ex, ++currentIndex, format);
            return this;
        }

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="converter">
        /// Custom function to write the member value into span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthWriterSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, FuncSpanTIntBool<R> converter = null)
        {
            indexed.Map(ex, ++currentIndex, converter);
            return this;
        }

        public IVariableLengthWriterSequentialBuilder<T> Map(Expression<Func<T, string>> ex, FuncSpanTIntBool converter = null)
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
        public IVariableLengthWriterSequentialBuilder<T> Skip(int columnCount)
        {
            currentIndex += columnCount;
            return this;
        }

    }
}
