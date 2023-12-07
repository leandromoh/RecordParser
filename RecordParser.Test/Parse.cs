using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RecordParser.Test
{
    internal static class Parse
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ProcessSpan(ReadOnlySpan<char> span) => span.ToString();
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> ProcessSpan(ReadOnlySpan<char> span) => span;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => int.Parse(ProcessSpan(utf8Text), NumberStyles.Integer, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Int64(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => long.Parse(ProcessSpan(utf8Text), NumberStyles.Integer, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime DateTimeExact(ReadOnlySpan<char> utf8Text, string format, IFormatProvider provider = null) => DateTime.ParseExact(ProcessSpan(utf8Text), format, provider, DateTimeStyles.AllowWhiteSpaces);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Decimal(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => decimal.Parse(ProcessSpan(utf8Text), NumberStyles.Number, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TEnum Enum<TEnum>(ReadOnlySpan<char> utf8Text) where TEnum : struct, Enum
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            return (TEnum)System.Enum.Parse(typeof(TEnum), utf8Text.ToString());
#elif NETSTANDARD2_1
            return System.Enum.Parse<TEnum>(utf8Text.ToString());
#else
            return System.Enum.Parse<TEnum>(utf8Text);
#endif
        }
    }
}
