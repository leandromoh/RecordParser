using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;

namespace RecordParser.Extensions
{
    public static partial class Exasd
    {
        // 104
        public static IEnumerable<T> GetRecords<T>(this IVariableLengthReader<T> reader, Stream stream, Encoding encoding, bool hasHeader)
        {
            return new RowByLine(stream, encoding).GetRecords().Skip(hasHeader ? 1 : 0).Select(item =>
            {
                var line = item.buffer.AsSpan().Slice(0, item.length);

                var res = reader.Parse(line);

                return res;
            });
        }

        // 108
        public static async IAsyncEnumerable<T> GetRecordsAsync<T>(this IVariableLengthReader<T> reader, Stream stream, Encoding encoding, bool hasHeader)
        {
            await using var e = new RowByLine(stream, encoding).GetRecordsAsync().GetAsyncEnumerator();

            if (hasHeader && await e.MoveNextAsync() == false)
                yield break;

            while (await e.MoveNextAsync())
            {
                var item = e.Current;

                var line = item.buffer.AsMemory().Slice(0, item.length);

                var res = reader.Parse(line.Span);

                yield return res;
            }
        }

        private class RowByLine
        {
            private readonly Encoding _encoding;
            private readonly Stream _stream;

            private byte[] _byteBuffer;
            private char[] _charBuffer;

            public RowByLine(Stream stream, Encoding encoding)
            {
                _encoding = encoding;
                _stream = stream;

                _byteBuffer = ArrayPool<byte>.Shared.Rent(512);
                _charBuffer = ArrayPool<char>.Shared.Rent(512);
            }

            public IEnumerable<(char[] buffer, int length)> GetRecords()
            {
                PipeReader reader = PipeReader.Create(_stream);

                while (true)
                {
                    ReadResult read = reader.ReadAsync().GetAwaiter().GetResult();
                    ReadOnlySequence<byte> buffer = read.Buffer;
                    while (ParallelRow.TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence))
                    {
                        var length = ProcessSequence(sequence);

                        yield return (_charBuffer, length);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                    if (read.IsCompleted)
                    {
                        break;
                    }
                }
            }

            public async IAsyncEnumerable<(char[] buffer, int length)> GetRecordsAsync()
            {
                PipeReader reader = PipeReader.Create(_stream);

                while (true)
                {
                    ReadResult read = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = read.Buffer;
                    while (ParallelRow.TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence))
                    {
                        var length = ProcessSequence(sequence);

                        yield return (_charBuffer, length);
                    }

                    reader.AdvanceTo(buffer.Start, buffer.End);
                    if (read.IsCompleted)
                    {
                        break;
                    }
                }
            }

            private int ProcessSequence(ReadOnlySequence<byte> sequence)
            {
                if (sequence.IsSingleSegment)
                {
                    return Parse(sequence.FirstSpan);
                }

                var sequenceLength = (int)sequence.Length;

                if (sequenceLength > _byteBuffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(_byteBuffer);
                    _byteBuffer = ArrayPool<byte>.Shared.Rent(sequenceLength);
                }

                var bytes = _byteBuffer.AsSpan().Slice(0, sequenceLength);

                sequence.CopyTo(bytes);

                return Parse(bytes);
            }

            private int Parse(ReadOnlySpan<byte> bytes)
            {
                if (bytes.Length > _charBuffer.Length)
                {
                    ArrayPool<char>.Shared.Return(_charBuffer);
                    _charBuffer = ArrayPool<char>.Shared.Rent(bytes.Length);
                }

                var chars = _charBuffer.AsSpan().Slice(0, bytes.Length);

                _encoding.GetChars(bytes, chars);

                return bytes.Length;
            }
        }
    }
}
