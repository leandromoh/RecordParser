using RecordParser.Extensions.FileReader.RowReaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RecordParser.Extensions.FileReader
{
    public class ParallelOptions
    {
        /// <summary>
        /// Indicates if the processing should be performed 
        /// in a parallel instead of sequential.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Indicates if the original ordering of records must be maintained.
        /// </summary>
        public bool EnsureOriginalOrdering { get; set; } = true;

        /// <summary>
        /// Degree of parallelism is the maximum number of concurrently 
        /// executing tasks that will be used to process the records.
        /// </summary>
        public int? DegreeOfParallelism { get; set; }

        /// <summary>
        /// The CancellationToken to associate with the parallel processing.
        /// </summary>
        public CancellationToken? CancellationToken { get; set; }
    }

    internal static class ReaderCommon
    {
        public static readonly int Length = (int)Math.Pow(2, 23);

        private static IEnumerable<T> Skip<T>(this IEnumerable<T> source, bool hasHeader) =>
            hasHeader
            ? source.Skip(1)
            : source;

        private static ParallelQuery<T> AsParallel<T>(this IEnumerable<T> source, ParallelOptions option)
        {
            var query = source.AsParallel();

            if (option.EnsureOriginalOrdering)
                query = query.AsOrdered();

            if (option.DegreeOfParallelism is { } degree)
                query = query.WithDegreeOfParallelism(degree);

            if (option.CancellationToken is { } token)
                query = query.WithCancellation(token);

            return query;
        }

        public static IEnumerable<T> GetRecordsParallel<T>(
            Func<ReadOnlyMemory<char>, int, T> reader,
            Func<IFL> getItems,
            bool hasHeader,
            ParallelOptions parallelOptions)
        {
            using var items = getItems();

            if (items.FillBuffer() <= 0)
            {
                yield break;
            }

            foreach (var x in items.ReadLines().Skip(hasHeader).AsParallel(parallelOptions).Select(reader))
            {
                yield return x;
            }

            while (items.FillBuffer() > 0)
            {
                foreach (var x in items.ReadLines().AsParallel(parallelOptions).Select(reader))
                {
                    yield return x;
                }
            }
        }

        public static IEnumerable<T> GetRecordsSequential<T>(Func<ReadOnlyMemory<char>, int, T> reader, Func<IFL> getItems, bool hasHeader)
        {
            using var items = getItems();

            if (items.FillBuffer() <= 0)
            {
                yield break;
            }

            var i = 0;
            foreach (var x in items.ReadLines().Skip(hasHeader))
            {
                yield return reader(x, i++);
            }

            while (items.FillBuffer() > 0)
            {
                i = 0;
                foreach (var x in items.ReadLines())
                {
                    yield return reader(x, i++);
                }
            }
        }
    }
}
