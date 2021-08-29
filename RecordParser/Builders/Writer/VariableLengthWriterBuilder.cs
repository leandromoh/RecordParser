using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Buffers;
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

        IVariableLengthWriterBuilder<T> Map(Expression<Func<T, string>> ex, int indexColumn, FuncSpanTIntBool converter = null);
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

        public IVariableLengthWriterBuilder<T> Map(Expression<Func<T, string>> ex, int indexColumn, FuncSpanTIntBool converter = null)
        {
            var member = ex.Body;
            var config = new MappingWriteConfiguration(member, indexColumn, null, converter, null, default, default, typeof(ReadOnlySpan<char>));
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
            var quote = '"';

            var maps = MappingWriteConfiguration.Merge(list.Select(x => x.Value), dic);

            var fmaks = new Dictionary<Delegate, Delegate>();

            var fnull = Quote(quote, separator);

            foreach (var x in maps)
                if (x.converter is FuncSpanTIntBool f)
                    fmaks[x.converter] = new FuncSpanTIntBool(
#if NET6_0
        [SkipLocalsInit]
#endif
                    (Span<char> span, ReadOnlySpan<char> text) =>
                    {
                        char[] array = null;
                        try
                        {
                            var newLengh = MinLengthToQuote(text, separator, quote);

                            Span<char> temp = newLengh > 128
                                                ? array = ArrayPool<char>.Shared.Rent(newLengh)
                                                : stackalloc char[newLengh];

                            var (success, written) = TryFormat(text, temp, quote, newLengh);
                            Debug.Assert(success);
                            return f(span, temp.Slice(0, written));
                        }
                        finally
                        {
                            if (array != null)
                                ArrayPool<char>.Shared.Return(array);
                        }
                    });

            var map2 = maps.Select(i =>
            {
                if (i.type != typeof(ReadOnlySpan<char>))
                    return i;

                var fmask = i.converter is null ? fnull : fmaks[i.converter];

                return new MappingWriteConfiguration(i.prop, i.start, null, fmask, null, default, default, i.type);
            });

            var expression = VariableLengthWriterEngine.GetFuncThatSetProperties<T>(map2, cultureInfo);

            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }

        private static FuncSpanTIntBool Quote(char quote, string separator)
        {
            return (Span<char> span, ReadOnlySpan<char> text) =>
            {
                if (text.Length > span.Length)
                {
                    return (false, 0);
                }

                var newLengh = MinLengthToQuote(text, separator, quote);

                return TryFormat(text, span, quote, newLengh);
            };
        }

        private static int MinLengthToQuote(ReadOnlySpan<char> text, ReadOnlySpan<char> separator, char quote)
        {
            var quoteFounds = 0;
            var containsSeparator = false;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == quote)
                {
                    quoteFounds++;
                    continue;
                }

                if (containsSeparator == false && text.Slice(i).StartsWith(separator))
                {
                    containsSeparator = true;
                }
            }

            if (quoteFounds == 0)
            {
                return containsSeparator ? text.Length + 2 : text.Length;
            }
            else
            {
                return text.Length + quoteFounds + 2;
            }
        }

        private static (bool, int) TryFormat(ReadOnlySpan<char> text, Span<char> span, char quote, int newLengh)
        {
            if (newLengh > span.Length)
            {
                return (false, 0);
            }

            if (newLengh == text.Length)
            {
                text.CopyTo(span);
                return (true, newLengh);
            }

            if (newLengh == text.Length + 2)
            {
                span[0] = quote;
                text.CopyTo(span.Slice(1));
                span[text.Length + 1] = quote;
                return (true, newLengh);
            }

            else
            {
                var j = 0;

                span[j++] = quote;

                for (var i = 0; i < text.Length; i++, j++)
                {
                    span[j] = text[i];

                    if (text[i] == quote)
                    {
                        span[++j] = quote;
                    }
                }

                span[j++] = quote;

                Debug.Assert(j == newLengh);

                return (true, newLengh);
            }
        }
    }
}
