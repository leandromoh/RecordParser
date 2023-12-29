#if NETSTANDARD2_0 || NETFRAMEWORK

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    internal static class Shims
    {
        public static int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
        {
            unsafe
            {
                fixed (byte* b = &MemoryMarshal.GetReference(bytes))
                {
                    int charCount = encoding.GetCharCount(b, bytes.Length);
                    if (charCount > chars.Length) return 0;

                    fixed (char* c = &MemoryMarshal.GetReference(chars))
                    {
                        return encoding.GetChars(b, bytes.Length, c, chars.Length);
                    }
                }
            }
        }

        public static string GetString(this Encoding encoding, scoped ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty) return string.Empty;

            unsafe
            {
                fixed (byte* pB = &MemoryMarshal.GetReference(bytes))
                {
                    return encoding.GetString(pB, bytes.Length);
                }
            }
        }

        public static Task WriteLineAsync(this TextWriter writer, ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(value, out var arraySegment))
            {
                return arraySegment.Array is null ? Task.CompletedTask : writer.WriteLineAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
            }

            return Impl(writer, value);

            static async Task Impl(TextWriter writer, ReadOnlyMemory<char> value)
            {
                var pool = ArrayPool<char>.Shared;
                var array = pool.Rent(value.Length);
                try
                {
                    value.CopyTo(array.AsMemory());
                    await writer.WriteLineAsync(array, 0, value.Length);
                }
                finally
                {
                    pool.Return(array);
                }
            }
        }
    }
}

#endif
