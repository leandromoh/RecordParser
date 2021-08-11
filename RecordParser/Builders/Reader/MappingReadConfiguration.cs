using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    internal readonly struct MappingReadConfiguration
    {
        public Expression prop { get; }
        public int start { get; }
        public int? length { get; }
        public Delegate fmask { get; }
        public Type type { get; }

        public bool ShouldTrim =>
            prop.Type == typeof(string) ||
            prop.Type == typeof(char) ||
           (prop.Type == typeof(DateTime) && fmask != null);

        public MappingReadConfiguration(Expression prop, int start, int? length, Type type, Delegate fmask)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.type = type;
            this.fmask = fmask;
        }

        public static IEnumerable<MappingReadConfiguration> Merge(
            IEnumerable<MappingReadConfiguration> list,
            IReadOnlyDictionary<Type, Delegate> dic)
        {
            var result = dic.Count is 0
                    ? list
                    : list.Select(i =>
                    {
                        if (i.fmask != null || !dic.TryGetValue(i.type, out var fmask))
                            return i;

                        return new MappingReadConfiguration(i.prop, i.start, i.length, i.type, fmask);
                    });

            result = result
                .OrderBy(x => x.start)
                .ToList();

            return result;
        }
    }
}
