namespace RecordParser
{
    using System;
    using System.Threading;

    public delegate string StringFactory(ReadOnlySpan<char> text);

    namespace RecordParser.Extensions.FileReader
    {
        public class ThreadSafeCache
        {
            private readonly int _maxParallelism;
            private readonly Foo[] _pool;
            private int _i;

            public ThreadSafeCache(Func<StringFactory> fac, int maxParallelism)
            {
                _i = -1;
                _maxParallelism = maxParallelism;
                _pool = new Foo[maxParallelism];
                for (int i = 0; i < maxParallelism; i++)
                {
                    _pool[i] = new Foo { lockObj = new object(), fac = fac() };
                }
            }

            public string Get(ReadOnlySpan<char> text)
            {
                return Get(text, Interlocked.Increment(ref _i));
            }

            public string Get(ReadOnlySpan<char> text, int i)
            {
                var r = _pool[i % _maxParallelism];
                lock (r.lockObj)
                {
                    return r.fac(text);
                }
            }

            private class Foo
            {
                public object lockObj;
                public StringFactory fac;
            }
        }
    }
}
