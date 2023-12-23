using System.Collections.Generic;

namespace RecordParser.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using var e = source.GetEnumerator();
            var hasMore = true;
            bool MoveNext() => hasMore && (hasMore = e.MoveNext());

            while (MoveNext())
                yield return Chunk(batchSize);

            IEnumerable<T> Chunk(int countdown)
            {
                do
                    yield return e.Current;
                while (--countdown > 0 && MoveNext());
            }
        }
    }
}
