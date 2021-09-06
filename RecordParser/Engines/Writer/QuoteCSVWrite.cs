using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RecordParser.Engines.Writer
{
    internal static class QuoteCSVWrite
    {
        public static IEnumerable<MappingWriteConfiguration> MagicQuote(this IEnumerable<MappingWriteConfiguration> maps, char quote, string separator)
        {
            var fmaks = new Dictionary<Delegate, Delegate>();
            var fnull = Quote(quote, separator);

            foreach (var x in maps)
                if (x.converter is FuncSpanTIntBool f)
                    fmaks[x.converter] = f.Quote(quote, separator);

            var map2 = maps.Select(i =>
            {
                if (i.type != typeof(ReadOnlySpan<char>))
                    return i;

                var fmask = i.converter is null ? fnull : fmaks[i.converter];

                return new MappingWriteConfiguration(i.prop, i.start, i.length, fmask, i.format, i.padding, i.paddingChar, i.type);
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
#if NET6_0
        [SkipLocalsInit]
#endif
                (Span<char> span, ReadOnlySpan<char> text) =>
                {
                    char[] array = null;
                    try
                    {
                        var newLengh = MinLengthToQuote(text, separator, quote);

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
