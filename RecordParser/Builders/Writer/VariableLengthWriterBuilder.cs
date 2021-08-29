using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly Dictionary<Type, Delegate> dic = new();

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
            var member = ex.Body;
            var config = new MappingWriteConfiguration(member, indexColumn, null, converter, null, default, default, typeof(R));
            list.Add(indexColumn, config);
            return this;
        }

        //public IVariableLengthWriterBuilder<T> Map(Expression<Func<T, string>> ex, int indexColumn, FuncSpanTIntBool converter)
        //{
        //    var member = ex.Body;
        //    var config = new MappingWriteConfiguration(member, indexColumn, null, converter == null ? null : new FuncSpanTIntBool<string>(
        //        (span, int) =>
        //        {



        //        }).WrapInLambdaExpression(), null, default, default, typeof(string));
        //    list.Add(indexColumn, config);
        //    return this;
        //}

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
            dic.Add(typeof(R), ex);
            return this;
        }

        /// <summary>
        /// Creates the writer object using the registered mappings.
        /// </summary>
        /// <param name="separator">The text (usually a character) that delimits collumns and separate values.</param>
        /// <param name="cultureInfo">Culture that will be used in the library internal default parsers functions.</param>
        /// <remarks>
        /// Culture passed will not be applied in custom parser functions registered by the user (neither for member or type).
        /// Culture should be applied manually inside these functions.
        /// </remarks>
        /// <returns>The writer object.</returns>
        public IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null)
        {
            var maps = MappingWriteConfiguration.Merge(list.Select(x => x.Value), dic);
            var expression = VariableLengthWriterEngine.GetFuncThatSetProperties<T>(maps, cultureInfo);

            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }

        /// <summary>
        /// Write a string into span as quoted field when
        /// 1) the string contains one or more quote character
        /// 2) the string contains the column delimiter
        /// </summary> 
        private static bool Quote(string text, Span<char> span, out int written)
        {
            if (text.Length > span.Length)
            {
                written = 0;
                return false;
            }

            char quote = '"';
            ReadOnlySpan<char> separator = " , ";

            var value = text.AsSpan();
            var length = value.Length;
            int i = 0;

            var quoteFounds = 0;
            var containsSeparator = false;

            for (; i < length; i++)
            {
                if (value[i] == quote)
                {
                    quoteFounds++;
                    continue;
                }

                if (containsSeparator == false && value.Slice(i).StartsWith(separator))
                {
                    containsSeparator = true;
                }
            }

            if (quoteFounds == 0)
            {
                if (containsSeparator)
                {
                    if (text.Length + 2 > span.Length)
                    {
                        written = 0;
                        return false;
                    }
                    else
                    {
                        span[0] = quote;
                        value.CopyTo(span.Slice(1));
                        span[value.Length + 1] = quote;

                        written = text.Length + 2;
                        return true;
                    }
                }
                else
                {
                    value.CopyTo(span);
                    written = value.Length;
                    return true;
                }
            }
            else
            {
                var newLengh = text.Length + quoteFounds + 2;

                if (newLengh > span.Length)
                {
                    written = 0;
                    return false;
                }
                else
                {
                    var j = 0;

                    span[++j] = quote;

                    for (i = 0; i < length; i++, j++)
                    {
                        span[j] = value[i];

                        if (value[i] == quote)
                        {
                            span[++j] = quote;
                        }
                    }

                    span[j] = quote;

                    Debug.Assert(j == newLengh);

                    written = newLengh;
                    return true;
                }
            }
        }
    }
}
