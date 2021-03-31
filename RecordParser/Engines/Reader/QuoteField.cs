using System;

namespace RecordParser.Engines.Reader
{
    public static class QuoteField
    {
        public static FuncSpanT<string> quote(this FuncSpanT<string> exp)
        {
            return (span) =>
            {
                const string quote = "\"\"";

                if (span.IndexOf(quote) == -1)
                {
                    return exp is null
                      ? new string(span)
                      : exp(span);
                }

                Span<char> resp = stackalloc char[span.Length];
                Span<char> e = resp;

                while (true)
                {
                    var pos = span.IndexOf(quote);
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
                }

                resp = resp.Slice(0, resp.Length - e.Length);

                return exp is null
                       ? new string(resp)
                       : exp(resp);
            };
        }
    }
}
