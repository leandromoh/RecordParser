using RecordParser.Engines;
using RecordParser.Engines.Reader;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IVariableLengthReaderBuilder<T>
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
        IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="convert">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanT<R> convert = null);
    }

    public class VariableLengthReaderBuilder<T> : IVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingReadConfiguration> list = new();
        private readonly Dictionary<Type, Delegate> dic = new();

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="convert">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body;
            var config = new MappingReadConfiguration(member, indexColumn, null, typeof(R), convert);
            list.Add(indexColumn, config);
            return this;
        }

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex);
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
        {
            QuoteHelper.ThrowIfSeparatorContainsQuote(separator);

            var quote = QuoteHelper.Quote.Char;

            var map = MappingReadConfiguration.Merge(list.Select(x => x.Value), dic);
            var func = ReaderEngine.RecordParserSpanCSV(map, factory);

            func = CultureInfoVisitor.ReplaceCulture(func, cultureInfo);

            return new VariableLengthReader<T>(func.Compile(), separator, quote);
        }
    }
}
