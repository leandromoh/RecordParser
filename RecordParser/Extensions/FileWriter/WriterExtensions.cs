using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecordParser.Extensions.FileWriter
{
    using RecordParser.Extensions.FileReader;

    /// <summary>
    /// Delegate representing object to text convert method.
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    /// <param name="instance">Instance that will be turn into text</param>
    /// <param name="destination">Destination buffer</param>
    /// <param name="charsWritten">Count of chars written into <paramref name="destination"/>.</param>
    /// <returns>
    /// True if the writting was succeeded, otherwise false.
    /// </returns>
    public delegate bool TryFormat<T>(T instance, Span<char> destination, out int charsWritten);

    public static class WriterExtensions
    {
        private const int initialPow = 10;

        /// <summary>
        /// Writes the elements of a sequence into the <paramref name="textWriter"/>.
        /// </summary>
        /// <typeparam name="T">Type of items in the sequence.</typeparam>
        /// <param name="textWriter">The TextWriter where the items will be written into.</param>
        /// <param name="items">Sequence of the elements.</param>
        /// <param name="tryFormat">Delegate that parses element into text.</param>
        public static void Write<T>(this TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat)
        {
            Write(textWriter, items, tryFormat, new ParallelOptions());
        }


        /// <summary>
        /// Writes the elements of a sequence into the <paramref name="textWriter"/>.
        /// </summary>
        /// <typeparam name="T">Type of items in the sequence.</typeparam>
        /// <param name="textWriter">The TextWriter where the items will be written into.</param>
        /// <param name="items">Sequence of the elements.</param>
        /// <param name="tryFormat">Delegate that parses element into text.</param>
        /// <param name="options">Options to configure parallel processing.</param>
        public static void Write<T>(this TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat, ParallelOptions options)
        {
            if (options.Enabled)
            {
                WriteParallel(textWriter, items, tryFormat, options);
            }
            else
            {
                WriteSequential(textWriter, items, tryFormat);
            }
        }

        private class BufferContext
        {
            public int pow;
            public char[] buffer;
            public object lockObj;
        }

        private static void WriteParallel<T>(TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat, ParallelOptions options)
        {
            var parallelism = 20; // TODO remove hardcoded
            var textWriterLock = new object();

            var buffers = Enumerable
                .Range(0, parallelism)
                .Select(_ => new BufferContext
                {
                    pow = initialPow,
                    buffer = ArrayPool<char>.Shared.Rent((int)Math.Pow(2, initialPow)),
                    lockObj = new object()
                })
                .ToArray();

            try
            {
                var xs = items.AsParallel(options).Select((item, i) =>
                {
                    var x = buffers[i % parallelism];

                    lock (x.lockObj)
                    {
                    retry:

                        if (tryFormat(item, x.buffer, out var charsWritten))
                        {
                            lock (textWriterLock)
                            {
                                textWriter.WriteLine(x.buffer, 0, charsWritten);
                            }
                        }
                        else
                        {
                            ArrayPool<char>.Shared.Return(x.buffer);
                            x.pow++;
                            x.buffer = ArrayPool<char>.Shared.Rent((int)Math.Pow(2, x.pow));
                            goto retry;
                        }
                    }

                    // dummy value
                    return (string)null;
                });

                // dummy iteration to force evaluation
                foreach (var _ in xs) ; 
            }
            finally
            {
                foreach (var x in buffers)
                    ArrayPool<char>.Shared.Return(x.buffer);
            }
        }

        private static void WriteSequential<T>(TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat)
        {
            var charsWritten = 0;
            var pow = initialPow;
            var buffer = ArrayPool<char>.Shared.Rent((int)Math.Pow(2, pow));

            try
            {
                foreach (var item in items)
                {
                retry:

                    if (tryFormat(item, buffer, out charsWritten))
                    {
                        textWriter.WriteLine(buffer, 0, charsWritten);
                    }
                    else
                    {
                        ArrayPool<char>.Shared.Return(buffer);
                        pow++;
                        buffer = ArrayPool<char>.Shared.Rent((int)Math.Pow(2, pow));
                        goto retry;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
    }
}
