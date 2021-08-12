using System;

namespace RecordParser.Engines.Reader
{
    internal static class QuoteField
    {
        public static (int start, int length) ParseQuotedChuck(bool isStr, in ReadOnlySpan<char> line, ref int scanned, ref int position, in ReadOnlySpan<char> delimiter)
        {
            const char singleQuote = '"';

            var unlook = line.Slice(scanned);

            scanned += unlook.IndexOf(singleQuote) + 1;
            unlook = line.Slice(scanned);
            position = 0;

            while (true)
            {
                position += unlook.Slice(position).IndexOf(singleQuote);
                position++;
                if (unlook.Slice(position) is var temp
                    && temp.IsEmpty == false
                    && temp[0] == singleQuote)
                {
                    position++;
                    continue;
                }

                return isStr 
                    ? (scanned - 1, position + 1)
                    : (scanned, position - 1);
            }
        }

        public static FuncSpanT<string> quote(this FuncSpanT<string> exp)
        {
            return (span) =>
            {
                if (span.IsEmpty || span[0] != '"')
                {
                    return done(exp, span);
                }
                else
                {
                    span = span.Slice(1, span.Length - 2);
                }

                const string quote = "\"\"";

                var pos = span.IndexOf(quote);

                if (pos == -1)
                    return done(exp, span);

                Span<char> resp = stackalloc char[span.Length];
                Span<char> e = resp;

                do
                {
                    if (pos != -1)
                    {
                        var temp = span.Slice(0, pos + 1);
                        temp.CopyTo(e);
                        e = e.Slice(temp.Length);
                        span = span.Slice(temp.Length + 1);
                    }
                    else
                    {
                        span.CopyTo(e);
                        e = e.Slice(span.Length);
                        break;
                    }

                    pos = span.IndexOf(quote);

                } while (true);

                resp = resp.Slice(0, resp.Length - e.Length);

                return done(exp, resp);
            };

            static string done(FuncSpanT<string> exp, ReadOnlySpan<char> resp)
            {
                return exp is null
                       ? new string(resp)
                       : exp(resp);
            }
        }
    }
}
