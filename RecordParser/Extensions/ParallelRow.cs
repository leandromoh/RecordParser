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
		// 45
		public static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, Stream stream, Encoding encoding, bool hasHeader)
		{
			return new ParallelRow(stream, encoding).GetRecords().Skip(hasHeader ? 1 : 0).AsParallel().Select(item =>
			{
				var line = item.buffer.AsSpan().Slice(0, item.length);

				var res = reader.Parse(line);

				ArrayPool<char>.Shared.Return(item.buffer);

				return res;
			});
		}

		private class ParallelRow
		{
			private readonly Encoding _encoding;
			private readonly Stream _stream;

			public ParallelRow(Stream stream, Encoding encoding)
			{
				_encoding = encoding;
				_stream = stream;
			}

			public IEnumerable<(char[] buffer, int length)> GetRecords()
			{
				var reader = PipeReader.Create(_stream);

				while (true)
				{
					ReadResult read = reader.ReadAsync().GetAwaiter().GetResult();
					ReadOnlySequence<byte> buffer = read.Buffer;
					while (TryReadLine(ref buffer, out ReadOnlySequence<byte> sequence))
					{
						var array = ArrayPool<char>.Shared.Rent(512);
						var item = ProcessSequence(sequence, ref array);

						yield return (array, item);
					}

					reader.AdvanceTo(buffer.Start, buffer.End);
					if (read.IsCompleted)
					{
						break;
					}
				}
			}

			public static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
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

			private static int ProcessSequence(ReadOnlySequence<byte> sequence, ref char[] parser)
			{
				if (sequence.IsSingleSegment)
				{
					return Parse(sequence.FirstSpan, ref parser);
				}

				var length = (int)sequence.Length;
				byte[] byteBuffer = null;


				Span<byte> span = length > 512
							 ? byteBuffer = ArrayPool<byte>.Shared.Rent(length)
							 : stackalloc byte[length];

				span = span.Slice(0, length);

				sequence.CopyTo(span);

				var res = Parse(span, ref parser);

				if (byteBuffer != null)
					ArrayPool<byte>.Shared.Return(byteBuffer);

				return res;
			}

			private static int Parse(ReadOnlySpan<byte> bytes, ref char[] chars)
			{
				if (bytes.Length > chars.Length)
				{
					ArrayPool<char>.Shared.Return(chars);
					chars = ArrayPool<char>.Shared.Rent(bytes.Length);
				}

				Encoding.UTF8.GetChars(bytes, chars);

				return bytes.Length;
			}
		}
	}
}
