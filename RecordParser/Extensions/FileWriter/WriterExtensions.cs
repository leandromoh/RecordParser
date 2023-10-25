using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        private class BufferContext
        {
            public char[] buffer;
            public object lockObj;
            public int charsWritten;
        }

        private static void WriteParallel<T>(TextWriter textWriter, IEnumerable<T> items, TryFormat<T> tryFormat, ParallelismOptions options)
        {
            // The largest number of partitions that PLINQ supports.
            const int MAX_SUPPORTED_DOP = 512;
            var defaultDegreeOfParallelism = options.MaxDegreeOfParallelism ?? Math.Min(Environment.ProcessorCount, MAX_SUPPORTED_DOP);
            defaultDegreeOfParallelism = 30;// Math.Min(defaultDegreeOfParallelism * 3, MAX_SUPPORTED_DOP);

            var size = defaultDegreeOfParallelism;
            var pool = new BufferContext[size];
            var bucket = new T[size];

            for (var i = 0; i < pool.Length; i++)
                pool[i] = new() { buffer = ArrayPool<char>.Shared.Rent((int)Math.Pow(2, initialPow)), lockObj = new object() };

            var count = 0;

            foreach (var item in items)
            {
                bucket[count++] = item;

                // The bucket is fully buffered before it's yielded
                if (count != size)
                    continue;

                Make();
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (count > 0)
            {
                Make();
            }

            foreach (var x in pool)
            {
                ArrayPool<char>.Shared.Return(x.buffer);
            }

            void Make()
            {
                var mem = bucket.AsMemory(0, count);
                Parallel.For(0, mem.Length, i =>
                {
                retry:
                    var x = mem.Span[i];
                    var r = pool[i];

                    if (tryFormat(x, r.buffer, out r.charsWritten))
                    {
                        return;
                    }
                    else
                    {
                        var newLength = r.buffer.Length * 2;
                        ArrayPool<char>.Shared.Return(r.buffer);
                        r.buffer = ArrayPool<char>.Shared.Rent(newLength);
                        goto retry;
                    }
                });

                for (int i = 0; i < mem.Length; i++)
                {
                    var r = pool[i];
                    textWriter.WriteLine(r.buffer, 0, r.charsWritten);
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
