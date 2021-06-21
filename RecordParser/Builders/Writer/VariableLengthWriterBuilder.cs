using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public interface IVariableLengthWriterBuilder<T>
    {
        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits collumns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// If a custom parser function was registered by the user (for member or type), 
        /// this culture will not be applied. Culture should be manually applied in custom parse functions. 
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
        IVariableLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format);

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="converter">
        /// Custom function to write the member value into span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanTIntBool<R> converter = null);
    }

    public class VariableLengthWriterBuilder<T> : IVariableLengthWriterBuilder<T>
    {
        private readonly Dictionary<int, MappingWriteConfiguration> list = new();
        private readonly Dictionary<Type, Func<Expression, Expression, Expression, Expression>> dic = new();

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="converter">
        /// Custom function to write the member value into span.
        /// The function must return 2 values to indicate if it was successful and how many characters were written.
        /// The success value is used to decide if the write operation should advance to next field or be stopped.
        /// The charsWritten value is used to calculate the number of non-written positions.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanTIntBool<R> converter = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, null, converter.WrapInLambdaExpression(), null, default, default, typeof(R));
            list.Add(indexColumn, config);
            return this;
        }

        /// <summary>
        /// Customize configuration for individual member.
        /// </summary>
        /// <param name="ex">
        /// An expression that identifies the property or field that will be assigned.
        /// </param>
        /// <param name="indexColumn">The zero-based position of the collumn where the field is located.</param>
        /// <param name="format">
        /// A standard or custom format for type <typeparamref name="R"/>.
        /// </param>
        /// <returns>An object to configure the mapping.</returns>
        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, null, null, format, default, default, typeof(R));
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
        public IVariableLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits collumns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// If a custom parser function was registered by the user (for member or type), 
        /// this culture will not be applied. Culture should be manually applied in custom parse functions. 
        /// </remarks>
        /// <returns>The writer object.</returns>
        public IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null)
        {
            var maps = MappingWriteConfiguration.Merge(list.Select(x => x.Value), dic);
            var expression = VariableLengthWriterEngine.GetFuncThatSetProperties<T>(maps, cultureInfo);

            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }
    }
}
