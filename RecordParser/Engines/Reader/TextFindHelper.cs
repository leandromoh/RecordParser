using System;
using System.Buffers;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace RecordParser.Engines.Reader
{
    internal ref struct TextFindHelper
    {
        private readonly ReadOnlySpan<char> line;
        private readonly string delimiter;
        private readonly (char ch, string str) quote;

#if NETCOREAPP3_1_OR_GREATER
        private readonly Vector128<ushort>? carriageReturnMaskVector;
#endif

        private int scanned;
        private int position;
        private int currentIndex;
        private ReadOnlySpan<char> currentValue;

        private char[] buffer;

        public TextFindHelper(ReadOnlySpan<char> source, string delimiter, (char ch, string str) quote)
        {
            this.line = source;
            this.delimiter = delimiter;
            this.quote = quote;

            scanned = -delimiter.Length;
            position = 0;
            currentIndex = -1;
            currentValue = default;
            buffer = null;

#if NETCOREAPP3_1_OR_GREATER
            carriageReturnMaskVector =
                delimiter.Length == 1 && Sse2.IsSupported
                ? Vector128.Create((ushort)(delimiter[0]))
                : null;
#endif
        }

        public void Dispose()
        {
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
                buffer = null;
            }
        }

        public ReadOnlySpan<char> GetValue(int index)
        {
            if (index <= currentIndex)
            {
                if (index == currentIndex)
                    return currentValue;
                else
                    throw new Exception("can only be forward");
            }

            while (currentIndex <= index)
            {
                var match = index == ++currentIndex;
                currentValue = ParseChunk(match);

                if (match)
                {
                    return currentValue;
                }
            }

            throw new Exception("invalid index for line");
        }

        private ReadOnlySpan<char> ParseChunk(bool match)
        {
            scanned += position + delimiter.Length;

            var unlook = line.Slice(scanned);
            var isQuotedField = unlook.TrimStart().StartsWith(quote.str);

            if (isQuotedField)
            {
                return ParseQuotedChuck(match);
            }

#if NETCOREAPP3_1_OR_GREATER
            if (carriageReturnMaskVector.HasValue)
            {
                return GoAheadUsingSIMD();
            }
#endif

            position = unlook.IndexOf(delimiter);
            if (position < 0)
            {
                position = line.Length - scanned;
            }

            return line.Slice(scanned, position);
        }
#if NETCOREAPP3_1_OR_GREATER

        public unsafe ReadOnlySpan<char> GoAheadUsingSIMD()
        {
            var del = carriageReturnMaskVector.Value;
            var unlook = line.Slice(scanned);
            var end = unlook.Length - 8;

            var i = 0;

            fixed (char* p = unlook)
            {
                ushort* ip = (ushort*)p;
                while (i < end)
                {
                    var dataVector = Sse2.LoadVector128(ip + i);
                    var delimiterVector = Sse2.CompareEqual(dataVector, del);

                    var delimiterMask = (uint)Sse2.MoveMask(delimiterVector.AsByte());

                    if (delimiterMask != 0)
                        break;
                    else
                        i += 8;
                }
            }

            var offset = unlook.Slice(i).IndexOf(delimiter[0]);

            position = offset < 0
                ? line.Length - scanned
                : i + offset;

            return line.Slice(scanned, position);
        }
#endif

        private ReadOnlySpan<char> ParseQuotedChuck(bool match)
        {
            const string corruptFieldError = "Double quote is not escaped or there is extra data after a quoted field.";

            var unlook = line.Slice(scanned);
            scanned += unlook.IndexOf(quote.ch) + 1;
            unlook = line.Slice(scanned);
            position = 0;

            if (match)
            {
                buffer ??= ArrayPool<char>.Shared.Rent(unlook.Length);
                Span<char> resp = buffer;

                for (int i = 0, j = 0; i < unlook.Length; i++)
                {
                    var c = unlook[i];

                    if (c == quote.ch)
                    {
                        var next = unlook.Slice(i + 1);
                        if (next.TrimStart().IsEmpty)
                        {
                            position += i;
                            return resp.Slice(0, j);
                        }
                        if (next[0] == quote.ch)
                        {
                            resp[j++] = quote.ch;
                            i++;
                            continue;
                        }

                        for (var t = 0; t < next.Length; t++)
                            if (next.Slice(t).StartsWith(delimiter))
                            {
                                position += i + 1 + t;
                                return resp.Slice(0, j);
                            }
                            else if (char.IsWhiteSpace(next[t]) is false)
                            {
                                break;
                            }

                        throw new Exception(corruptFieldError);

                    }
                    else
                    {
                        resp[j++] = c;
                        continue;
                    }
                }
            }
            else
            {
                for (int i = 0; i < unlook.Length; i++)
                {
                    if (unlook[i] == quote.ch)
                    {
                        var next = unlook.Slice(i + 1);
                        if (next.TrimStart().IsEmpty)
                        {
                            position += i;
                            return default;
                        }
                        if (next[0] == quote.ch)
                        {
                            i++;
                            continue;
                        }

                        for (var t = 0; t < next.Length; t++)
                            if (next.Slice(t).StartsWith(delimiter))
                            {
                                position += i + 1 + t;
                                return default;
                            }
                            else if (char.IsWhiteSpace(next[t]) is false)
                            {
                                break;
                            }

                        throw new Exception(corruptFieldError);

                    }
                    else
                    {
                        continue;
                    }
                }
            }

            throw new Exception("Quoted field is missing closing quote.");
        }
    }
}
