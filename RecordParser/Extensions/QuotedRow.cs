using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecordParser.Extensions
{
    public static partial class Exasd
	{
		static int length = (int)Math.Pow(2, 23);

		// 63
		public static IEnumerable<T> GetRecordsParallelCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
		{
			var items = new QuotedRow(stream, length);

			if (items.FillBufferAsync() > 0 == false)
			{
				yield break;
			}

			foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(x => reader.Parse(x.Span)))
			{
				yield return x;
			}

			while (items.FillBufferAsync() > 0)
			{
				foreach (var x in items.TryReadLine().AsParallel().Select(x => reader.Parse(x.Span)))
				{
					yield return x;
				}
			}
		}


		//public static IEnumerable<T> GetRecordsParallelList<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
		//{
		//	var items = new CsvUtility(stream, length);
		//	var result = new List<T>();

		//	if (items.FillBufferAsync() > 0 == false)
		//	{
		//		return result;
		//	}

		//	var itdx = items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(x =>
		//	{
		//		return reader.Parse(x.Span);
		//	});

		//	result.AddRange(itdx);

		//	while (items.FillBufferAsync() > 0)
		//	{
		//		var itd = items.TryReadLine().AsParallel().Select(x =>
		//		{
		//			return reader.Parse(x.Span);
		//		});

		//		result.AddRange(itd);
		//	}

		//	return result;
		//}


		// 127
		public static IEnumerable<T> GetRecordsSequentialCSV<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
		{
			var items = new QuotedRow(stream, length);

			if (items.FillBufferAsync() > 0 == false)
			{
				yield break;
			}

			foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0))
			{
				yield return reader.Parse(x.Span);
			}

			while (items.FillBufferAsync() > 0)
			{
				foreach (var x in items.TryReadLine())
				{
					yield return reader.Parse(x.Span);
				}
			}
		}

		private class QuotedRow
		{
			private int i = 0;
			private int j = 0;
			private State state = State.BeforeField;
			private int c;
			private TextReader reader;
			private bool initial = true;

			private int bufferLength;
			private char[] buffer;

			public QuotedRow(TextReader reader) : this(reader, (int)Math.Pow(2, 23))
			{

			}

			public QuotedRow(TextReader reader, int bufferLength)
			{
				this.reader = reader;

				buffer = ArrayPool<char>.Shared.Rent(bufferLength);
				this.bufferLength = buffer.Length;
			}

			private enum State
			{
				BeforeField,
				InField,
				InQuotedField,
				LineEnd,
			}

			public int FillBufferAsync()
			{
				var len = i - j;
				if (initial == false)
				{
					Array.Copy(buffer, j, buffer, 0, len);
				}

				var totalRead = reader.Read(buffer, len, bufferLength - len);
				bufferLength = len + totalRead;

				i = 0;
				j = 0;

				initial = false;

				return totalRead;
			}

			public IEnumerable<Memory<char>> TryReadLine()
			{
				int Peek() => i < bufferLength ? buffer[i] : -1;

				var hasBufferToConsume = false;

			reloop:

				j = i;
				state = State.BeforeField;

				while (hasBufferToConsume = i < bufferLength)
				{
					c = buffer[i++];

					switch (state)
					{
						case State.BeforeField:

							switch (c)
							{
								case '"':
									state = State.InQuotedField;
									break;
								case ',':
									//  fields.Add(string.Empty);
									break;
								case '\r':
									// fields.Add(string.Empty);
									if (Peek() == '\n')
									{
										i++;
									}
									state = State.LineEnd;
									goto afterLoop;

								case '\n':
									// fields.Add(string.Empty);
									state = State.LineEnd;
									goto afterLoop;

								default:
									// builder.Append((char)c);
									state = State.InField;
									break;
							}
							break;

						case State.InField:
							switch (c)
							{
								case ',':
									//  AddField(fields, builder);
									state = State.BeforeField;
									break;
								case '\r':
									//  AddField(fields, builder);
									if (Peek() == '\n')
									{
										i++;
									}
									state = State.LineEnd;
									goto afterLoop;

								case '\n':
									//    AddField(fields, builder);
									state = State.LineEnd;
									goto afterLoop;

								default:
									//      builder.Append((char)c);
									break;
							}
							break;

						case State.InQuotedField:
							switch (c)
							{
								case '"':
									var nc = Peek();
									switch (nc)
									{
										case '"':
											//        builder.Append('"');
											i++;

											break;
										case ',':
											i++;

											//           AddField(fields, builder);
											state = State.BeforeField;
											break;
										case '\r':
											i++;

											//          AddField(fields, builder);
											if (Peek() == '\n')
											{
												i++;
											}
											state = State.LineEnd;
											goto afterLoop;

										case '\n':
											i++;
											//          AddField(fields, builder);
											state = State.LineEnd;
											goto afterLoop;

										default:
											throw new InvalidDataException("Corrupt field found. A double quote is not escaped or there is extra data after a quoted field.");
									}
									break;
								default:
									//    builder.Append((char)c);
									break;
							}
							break;

						default:
							throw new NotImplementedException();
					}

					//if (state == State.LineEnd)
					//{
					//    if (i == 1)
					//        throw new Exception(); // goto reloop;

					//    break;
					//}
				}

			afterLoop:

				if (hasBufferToConsume == false)
				{
					yield break;

					if (FillBufferAsync() == 0)
					{
						ArrayPool<char>.Shared.Return(buffer);
						yield break;
					}

					goto reloop;
				}

				switch (state)
				{
					case State.BeforeField:
						yield return buffer.AsMemory(j, i - j);
						goto reloop;

					case State.LineEnd:
						if ((i == 1 && char.IsWhiteSpace(buffer[0])) == false)
							yield return buffer.AsMemory(j, i - j - 1);
						goto reloop;

					case State.InField:
						yield return buffer.AsMemory(j, i - j);
						goto reloop;

					case State.InQuotedField:
						break;
						throw new InvalidDataException("When the line ends with a quoted field, the last character should be an unescaped double quote.");
				}
			}
		}
	}
}
