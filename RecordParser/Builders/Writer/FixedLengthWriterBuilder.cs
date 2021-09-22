using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public interface IFixedLengthWriterBuilder<T>
    {
        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The writer object.</returns>
        IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null);

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <param name="padding">
        /// Defines which side padding will be applied.
        /// </param>
        /// <param name="paddingChar">
        /// The character that will be used to fill non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ');

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">
        /// Custom function to write the member value into its position inside span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <param name="padding">
        /// Defines which side padding will be applied.
        /// </param>
        /// <param name="paddingChar">
        /// The character that will be used to fill non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ');
    }

    public class FixedLengthWriterBuilder<T> : IFixedLengthWriterBuilder<T>
    {
        private readonly List<MappingWriteConfiguration> list = new();
        private readonly Dictionary<Type, Delegate> dic = new();

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="converter">
        /// Custom function to write the member value into its position inside span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <param name="padding">
        /// Defines which side padding will be applied.
        /// </param>
        /// <param name="paddingChar">
        /// The character that will be used to fill non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanTIntBool<R> converter = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var member = ex.Body;
            list.Add(new MappingWriteConfiguration(member, startIndex, length, converter, null, padding, paddingChar, typeof(R)));
            return this;
        }

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="startIndex">The zero-based position where the field starts.</param>
        /// <param name="length">The number of characters used by the field.</param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <param name="padding">
        /// Defines which side padding will be applied.
        /// </param>
        /// <param name="paddingChar">
        /// The character that will be used to fill non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string format, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var member = ex.Body;
            list.Add(new MappingWriteConfiguration(member, startIndex, length, null, format, padding, paddingChar, typeof(R)));
            return this;
        }

        /// <summary>
        /// Define a default custom function that will be used to parse all properties or fields of type <typeparamref name="R"/>,
        /// except whose configurated with a specific custom function.
        /// </summary>
        /// <typeparam name="R">The type that will have a default custom function configurated.</typeparam>
        /// <param name="ex">The default custom function for type <typeparamref name="R"/>.</param>
        /// <returns>An object to configure the mapping.</returns>
        public IFixedLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            dic.Add(typeof(R), ex);
            return this;
        }

        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The writer object.</returns>
        public IFixedLengthWriter<T> Build(CultureInfo cultureInfo = null)
        {
            var maps = MappingWriteConfiguration.Merge(list, dic);
            var expression = FixedLengthWriterEngine.GetFuncThatSetProperties<T>(maps, cultureInfo);

            return new FixedLengthWriter<T>(expression.Compile());
        }
    }
}
