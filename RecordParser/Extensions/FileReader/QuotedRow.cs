using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace RecordParser.Extensions.FileReader
{
    public static partial class Exasd
    {
        private class QuotedRow : IFL
        {
            private int i = 0;
            private int j = 0;
            private RowState state = RowState.BeforeField;
            private int c;
            private TextReader reader;
            private bool initial = true;

            private bool yieldLast = false;

            private int bufferLength;
            private char[] buffer;

            public QuotedRow(TextReader reader, int bufferLength)
            {
                this.reader = reader;

                buffer = ArrayPool<char>.Shared.Rent(bufferLength);
                this.bufferLength = buffer.Length;
            }

            private enum RowState
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

                if (totalRead == 0 && len != 0 && yieldLast == false)
                {
                    yieldLast = true;
                    return len;
                }

                return totalRead;
            }

            public IEnumerable<ReadOnlyMemory<char>> TryReadLine()
            {
                int Peek() => i < bufferLength ? buffer[i] : -1;

                var hasBufferToConsume = false;

            reloop:

                j = i;
                state = RowState.BeforeField;

                while (hasBufferToConsume = i < bufferLength)
                {
                    c = buffer[i++];

                    switch (state)
                    {
                        case RowState.BeforeField:

                            switch (c)
                            {
                                case '"':
                                    state = RowState.InQuotedField;
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
                                    state = RowState.LineEnd;
                                    goto afterLoop;

                                case '\n':
                                    // fields.Add(string.Empty);
                                    state = RowState.LineEnd;
                                    goto afterLoop;

                                default:
                                    // builder.Append((char)c);
                                    state = RowState.InField;
                                    break;
                            }
                            break;

                        case RowState.InField:
                            switch (c)
                            {
                                case ',':
                                    //  AddField(fields, builder);
                                    state = RowState.BeforeField;
                                    break;
                                case '\r':
                                    //  AddField(fields, builder);
                                    if (Peek() == '\n')
                                    {
                                        i++;
                                    }
                                    state = RowState.LineEnd;
                                    goto afterLoop;

                                case '\n':
                                    //    AddField(fields, builder);
                                    state = RowState.LineEnd;
                                    goto afterLoop;

                                default:
                                    //      builder.Append((char)c);
                                    break;
                            }
                            break;

                        case RowState.InQuotedField:
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
                                            state = RowState.BeforeField;
                                            break;
                                        case '\r':
                                            i++;

                                            //          AddField(fields, builder);
                                            if (Peek() == '\n')
                                            {
                                                i++;
                                            }
                                            state = RowState.LineEnd;
                                            goto afterLoop;

                                        case '\n':
                                            i++;
                                            //          AddField(fields, builder);
                                            state = RowState.LineEnd;
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
                    if (yieldLast)
                        yield return buffer.AsMemory(j, i - j);

                    yield break;

                    if (FillBufferAsync() == 0)
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                        yield break;
                    }

                    goto reloop;
                }

                if (state != RowState.InQuotedField)
                {
                    yield return buffer.AsMemory(j, i - j);
                    goto reloop;
                }
                else
                {
                    throw new InvalidDataException("When the line ends with a quoted field, the last character should be an unescaped double quote.");
                }
            }

            public void Dispose()
            {
                if (buffer != null)
                {
                    ArrayPool<char>.Shared.Return(buffer);
                    buffer = null;
                }
            }
        }
    }
}
