using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace RecordParser.Benchmark
{
    public static class Common
    {
        public const int BufferSize = 4_096; // 2^12

        public static async Task ProcessFile(string filePath, FuncSpanT<Person> parser, int limitRecord)
        {
            using var stream = File.OpenRead(filePath);
            PipeReader reader = PipeReader.Create(stream, new(null, BufferSize, 1024, false));

            var i = 0;

            while (true)
            {
                ReadResult read = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = read.Buffer;
                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence))
                {
                    if (i++ == limitRecord) return;

                    var person = ProcessSequence(sequence, parser);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (read.IsCompleted)
                {
                    break;
                }
            }
        }

        private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            var position = buffer.PositionOf((byte)'\n');
            if (position == null)
            {
                line = default;
                return false;
            }

            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

            return true;
        }

        private static T ProcessSequence<T>(ReadOnlySequence<byte> sequence, FuncSpanT<T> parser)
        {
            const int LengthLimit = 256;

            if (sequence.IsSingleSegment)
            {
                return Parse(sequence.FirstSpan, parser);
            }

            var length = (int)sequence.Length;
            if (length > LengthLimit)
            {
                throw new ArgumentException($"Line has a length exceeding the limit: {length}");
            }

            Span<byte> span = stackalloc byte[(int)sequence.Length];
            sequence.CopyTo(span);

            return Parse(span, parser);
        }

        private static T Parse<T>(ReadOnlySpan<byte> bytes, FuncSpanT<T> parser)
        {
            Span<char> chars = stackalloc char[bytes.Length];
            Encoding.UTF8.GetChars(bytes, chars);

            return parser(chars);
        }

        public static ReadOnlySpan<char> ParseChunk(ref ReadOnlySpan<char> span, ref int scanned, ref int position)
        {
            scanned += position + 1;

            position = span.Slice(scanned, span.Length - scanned).IndexOf(',');
            if (position < 0)
            {
                position = span.Slice(scanned, span.Length - scanned).Length;
            }

            return span.Slice(scanned, position);
        }
    }
}
