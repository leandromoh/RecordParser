using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RecordParser.Extensions.FileWriter
{
    using RecordParser.Extensions.FileReader;

    public delegate bool TryFormat<T>(T instance, Span<char> destination, out int charsWritten);

    public static class WriterExtensions
    {
        private const int initialPow = 10;

        public static void Write<T>(this IEnumerable<T> items, TextWriter textWriter, TryFormat<T> tryFormat)
        {
            items.Write(textWriter, tryFormat, new ParallelOptions());
        }

        public static void Write<T>(this IEnumerable<T> items, TextWriter textWriter, TryFormat<T> tryFormat, ParallelOptions options)
        {
            if (options.Enabled)
                WriteParallel(items, textWriter, tryFormat, options);
            else
                WriteSequential(items, textWriter, tryFormat);
        }

        private static void WriteParallel<T>(IEnumerable<T> items, TextWriter textWriter, TryFormat<T> tryFormat, ParallelOptions options)
        {
            var parallelism = 4;

            var buffers = Enumerable
                .Range(0, parallelism)
                .Select(_ => (pow: initialPow,
                              buffer: ArrayPool<char>.Shared.Rent((int)Math.Pow(2, initialPow)),
                              lockObj: new object()))
                .ToArray();

            var textWriterLock = new object();
            var parallelOptions = new System.Threading.Tasks.ParallelOptions();
            
            if (options.MaxDegreeOfParallelism is { } degree)
                parallelOptions.MaxDegreeOfParallelism = degree;

            if (options.CancellationToken is { } cancellationToken)
                parallelOptions.CancellationToken = cancellationToken;

            try
            {
                Parallel.ForEach(items, parallelOptions, (item, _, i) =>
                {
                    var x = buffers[i % parallelism];

                    lock (x.lockObj)
                    {
                        x = buffers[i % parallelism];

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

                            buffers[i % parallelism] = x;
                            goto retry;
                        }
                    }
                });
            }
            finally
            {
                foreach (var x in buffers)
                    ArrayPool<char>.Shared.Return(x.buffer);
            }
        }

        private static void WriteSequential<T>(IEnumerable<T> items, TextWriter textWriter, TryFormat<T> tryFormat)
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
