using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RecordParser.Benchmark
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
        public static byte Byte(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => byte.Parse(ProcessSpan(utf8Text), NumberStyles.Integer, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte SByte(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => sbyte.Parse(ProcessSpan(utf8Text), NumberStyles.Integer, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Double(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => double.Parse(ProcessSpan(utf8Text), NumberStyles.AllowThousands | NumberStyles.Float, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Single(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => float.Parse(ProcessSpan(utf8Text), NumberStyles.AllowThousands | NumberStyles.Float, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Int32(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => int.Parse(ProcessSpan(utf8Text), NumberStyles.Integer, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid Guid(ReadOnlySpan<char> utf8Text) => System.Guid.Parse(ProcessSpan(utf8Text));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime DateTime(ReadOnlySpan<char> utf8Text, IFormatProvider provider = null) => System.DateTime.Parse(ProcessSpan(utf8Text), provider, DateTimeStyles.AllowWhiteSpaces);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Boolean(ReadOnlySpan<char> utf8Text) => bool.Parse(ProcessSpan(utf8Text));

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
