using RecordParser.Builders.Writer;
using RecordParser.Engines.Reader;
using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        private class NodeState
        {
            public Type Type { get; set; }
            public Expression CurrentExpression { get; set; }
            public int Depth { get; set; }
        }

        /// <summary>
        /// Writes the elements of a sequence into the <paramref name="textWriter"/> as well the header of file.
        /// </summary>
        /// <typeparam name="T">Type of items in the sequence.</typeparam>
        /// <param name="textWriter">The TextWriter where the items will be written into.</param>
        /// <param name="items">Sequence of the elements.</param>
        /// <param name="options">Options to configure parallel processing.</param>
        public static void WriteRecords<T>(this TextWriter textWriter, IEnumerable<T> items, ParallelismOptions options)
        {
            const string separator = ";";
            var members = GetPropertyExpressions(typeof(T), 64);
            var builder = new VariableLengthWriterSequentialBuilder<T>();

            foreach (var item in members.Select(x => x.exp))
                if (item.ReturnType == typeof(string))
                    builder.Map((dynamic)item, converter: default(FuncSpanTIntBool));
                else
                    builder.Map((dynamic)item);

            var parser = builder.Build(separator);
            var header = string.Join(separator, members.Select(x => x.column));
            
            textWriter.WriteLine(header);
            WriteRecords(textWriter, items, parser.TryFormat, options);
        }

        private static IReadOnlyList<(LambdaExpression exp, string column)> GetPropertyExpressions(Type type, int maxDepth)
        {
            var expressions = new List<(LambdaExpression, string)>();

            if (maxDepth < 1)
                return expressions;

            var paramText = Guid.NewGuid().ToString();
            var rootParameter = Expression.Parameter(type, paramText);

            var queue = new Queue<NodeState>();

            queue.Enqueue(new NodeState
            {
                Type = type,
                CurrentExpression = rootParameter,
                Depth = 1
            });

            // loop BFS
            while (queue.Count > 0)
            {
                var currentState = queue.Dequeue();

                if (currentState.Depth > maxDepth)
                    continue;

                var properties = currentState.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (prop.CanRead == false)
                        continue;

                    var propertyAccess = Expression.Property(currentState.CurrentExpression, prop);

                    if (PrimitiveTypeReaderEngine.IsPrimitiveType(prop.PropertyType))
                    {
                        var lambda = Expression.Lambda(propertyAccess, rootParameter);
                        var column = propertyAccess.ToString().Replace(paramText + ".", string.Empty);
                        expressions.Add((lambda, column));
                    }
                    else
                    {
                        queue.Enqueue(new NodeState
                        {
                            Type = prop.PropertyType,
                            CurrentExpression = propertyAccess,
                            Depth = currentState.Depth + 1
                        });
                    }
                }
            }

            return expressions;
        }
    }
}
