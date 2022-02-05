using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RecordParser.Engines.Writer
{
    internal static class QuoteCSVWrite
    {
        public static IEnumerable<MappingWriteConfiguration> MagicQuote(this IEnumerable<MappingWriteConfiguration> maps, char quote, string separator)
        {
            var fmaks = new Dictionary<Delegate, Delegate>();
            var fnull = Quote(quote, separator);

            foreach (var x in maps)
            {
                if (x.converter is FuncSpanTIntBool f)
                    fmaks[x.converter] = f.Quote(quote, separator);

                else if (x.converter is FuncSpanTIntBool<string> s)
                    fmaks[x.converter] = s.Quote(quote, separator);
            }

            var map2 = maps.Select(i =>
            {
                if (i.type != typeof(ReadOnlySpan<char>) && i.type != typeof(string))
                    return i;

                var (fmask, type) = i.converter is null
                                    ? (fnull, typeof(ReadOnlySpan<char>))
                                    : (fmaks[i.converter], i.type);

                return new MappingWriteConfiguration(i.prop, i.start, i.length, fmask, i.format, i.padding, i.paddingChar, type);
            })
                .ToList();

            return map2;
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

        private static FuncSpanTIntBool Quote(this FuncSpanTIntBool f, char quote, string separator)
        {
            return
#if NET5_0_OR_GREATER
        [SkipLocalsInit]
#endif
                (Span<char> span, ReadOnlySpan<char> text) =>
                {
                    var newLengh = MinLengthToQuote(text, separator, quote);

                    if (newLengh == text.Length)
                        return f(span, text);

                    char[] array = null;

                    try
                    {
                        Span<char> temp = (newLengh > 128
                                            ? array = ArrayPool<char>.Shared.Rent(newLengh)
                                            : stackalloc char[newLengh])
                                          .Slice(0, newLengh);

                        var (success, written) = TryFormat(text, temp, quote, newLengh);

                        Debug.Assert(success);
                        Debug.Assert(written == newLengh);

                        return f(span, temp);
                    }
                    finally
                    {
                        if (array != null)
                            ArrayPool<char>.Shared.Return(array);
                    }
                };
        }

        private static FuncSpanTIntBool<string> Quote(this FuncSpanTIntBool<string> f, char quote, string separator)
        {
            return
#if NET5_0_OR_GREATER
        [SkipLocalsInit]
#endif
                (Span<char> span, string text) =>
                {
                    var newLengh = MinLengthToQuote(text, separator, quote);

                    if (newLengh == text.Length)
                        return f(span, text);

                    char[] array = null;

                    try
                    {
                        Span<char> temp = (newLengh > 128
                                            ? array = ArrayPool<char>.Shared.Rent(newLengh)
                                            : stackalloc char[newLengh])
                                          .Slice(0, newLengh);

                        var (success, written) = TryFormat(text, temp, quote, newLengh);

                        Debug.Assert(success);
                        Debug.Assert(written == newLengh);

                        return f(span, new string(temp));
                    }
                    finally
                    {
                        if (array != null)
                            ArrayPool<char>.Shared.Return(array);
                    }
                };
        }

        private static int MinLengthToQuote(ReadOnlySpan<char> text, ReadOnlySpan<char> separator, char quote)
        {
            if (text.IsEmpty)
                return 0;

            var quoteFounds = 0;
            var needQuoteAround = char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[text.Length - 1]);

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == quote)
                {
                    quoteFounds++;
                    continue;
                }

                if (needQuoteAround == false && (text.Slice(i).StartsWith(separator) ||
                                                 text[i] == ',' ||
                                                 text[i] == '\r' ||
                                                 text[i] == '\n'))
                {
                    needQuoteAround = true;
                }
            }

            if (quoteFounds == 0)
            {
                return needQuoteAround ? text.Length + 2 : text.Length;
            }
            else
            {
                return text.Length + quoteFounds + 2;
            }
        }

        private static (bool, int) TryFormat(ReadOnlySpan<char> text, Span<char> span, char quote, int newLength)
        {
            if (newLength > span.Length)
            {
                return (false, 0);
            }

            if (newLength == text.Length)
            {
                text.CopyTo(span);
                return (true, newLength);
            }

            if (newLength == text.Length + 2)
            {
                span[0] = quote;
                text.CopyTo(span.Slice(1));
                span[text.Length + 1] = quote;
                return (true, newLength);
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

                Debug.Assert(j == newLength);

                return (true, newLength);
            }
        }
    }
}
