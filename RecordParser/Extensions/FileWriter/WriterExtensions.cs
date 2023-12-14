using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RecordParser.Extensions
{
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
        public static void WriteRecords<T>(this TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat)
        {
            WriteRecords(textWriter, items, tryFormat, new ParallelismOptions());
        }


        /// <summary>
        /// Writes the elements of a sequence into the <paramref name="textWriter"/>.
        /// </summary>
        /// <typeparam name="T">Type of items in the sequence.</typeparam>
        /// <param name="textWriter">The TextWriter where the items will be written into.</param>
        /// <param name="items">Sequence of the elements.</param>
        /// <param name="tryFormat">Delegate that parses element into text.</param>
        /// <param name="options">Options to configure parallel processing.</param>
        public static void WriteRecords<T>(this TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat, ParallelismOptions options)
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

        private static void WriteParallel<T>(TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat, ParallelismOptions options)
        {
            var poolSize = 10_000;
            var pool = new char[poolSize][];

            for (var index = 0; index < poolSize; index++)
                pool[index] = new char[(int)Math.Pow(2, initialPow)];

            foreach (var xx in items.Batch(poolSize))
            {
                var xs = xx.AsParallel(options).Select((item, i) =>
                {
                    var buffer = pool[i];
                retry:

                    if (tryFormat(item, buffer, out var charsWritten))
                    {
                        return (buffer, charsWritten);
                    }
                    else
                    {
                        buffer = pool[i] = new char[buffer.Length * 2];
                        goto retry;
                    }
                });

                foreach (var x in xs)
                {
                    textWriter.WriteLine(x.buffer, 0, x.charsWritten);
                }
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
