#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RecordParser
{
    internal static class Shims
    {
        public static bool Contains(this string @this, char charToFind) => @this.IndexOf(charToFind) != -1;

        public static bool StartsWith(this ReadOnlySpan<char> @this, string value) => @this.StartsWith(value.AsSpan());

        public static bool EndsWith(this ReadOnlySpan<char> @this, string value) => @this.EndsWith(value.AsSpan());

        public static int IndexOf(this ReadOnlySpan<char> @this, string value) => @this.IndexOf(value.AsSpan());

        public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
        {
            if (stack.Count > 0)
            {
                result = stack.Pop();
                return true;
            }

            result = default;
            return false;
        }
    }
}

#endif
