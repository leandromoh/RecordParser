using RecordParser.Engines.Reader;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IFixedLengthReaderBuilder<T>
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
        IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanT<R> converter = null);
    }

    public class FixedLengthReaderBuilder<T> : IFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingReadConfiguration> list = new();
        private readonly Dictionary<Type, Delegate> dic = new();

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">Custom function to parse the ReadOnlySpan of char to <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            FuncSpanT<R> converter = null)
        {
            var member = ex.Body;
            list.Add(new MappingReadConfiguration(member, startIndex, length, typeof(R), converter));
            return this;
        }

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex);
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
        {
            var map = MappingReadConfiguration.Merge(list, dic);
            var func = ReaderEngine.RecordParserSpanFlat(map, factory);

            func = CultureInfoVisitor.ReplaceCulture(func, cultureInfo);

            return new FixedLengthReader<T>(func.Compile());
        }
    }
}
