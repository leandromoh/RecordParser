using System;

namespace RecordParser.Engines
{
    internal static class QuoteHelper
    {
        public static readonly (char Char, string String) Quote = ('"', "\"");

        public static void ThrowIfSeparatorContainsQuote(string separator)
        {
            if (separator.Contains(Quote.Char))
                throw new ArgumentException("Separator must not contain quote char", nameof(separator));
        }
    }
}
