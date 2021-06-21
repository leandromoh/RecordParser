﻿using RecordParser.Parsers;
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
        /// <remarks>
        /// If a custom parser function was registered by the user (for member or type), 
        /// this culture will not be applied. Culture should be manually applied in custom parse functions. 
        /// </remarks>
        /// <returns>The reader object.</returns>
        IFixedLengthReader<T> Build(CultureInfo cultureInfo = null);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose that were configurated with a specific custom function.
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
        /// <param name="convert">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, FuncSpanT<R> convert = null);
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
        /// <param name="convert">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int length,
            FuncSpanT<R> convert = null)
        {
            indexed.Map(ex, currentPosition, length, convert);
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
        /// except whose that were configurated with a specific custom function.
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
        /// <remarks>
        /// If a custom parser function was registered by the user (for member or type), 
        /// this culture will not be applied. Culture should be manually applied in custom parse functions. 
        /// </remarks>
        /// <returns>The reader object.</returns>
        public IFixedLengthReader<T> Build(CultureInfo cultureInfo = null) 
            => indexed.Build(cultureInfo);
    }
}
