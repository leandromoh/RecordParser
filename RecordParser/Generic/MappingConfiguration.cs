using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    internal readonly struct MappingConfiguration
    {
        public Expression prop { get; }
        public int start { get; }
        public int? length { get; }
        public Expression fmask { get; }
        public Type type { get; }

        public MappingConfiguration(Expression prop, int start, int? length, Type type, Expression fmask)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.type = type;
            this.fmask = fmask;
        }

        public static IEnumerable<MappingConfiguration> Merge(
            IEnumerable<MappingConfiguration> list,
            IReadOnlyDictionary<Type, Expression> dic)
        {
            var result = dic.Any() != true
                    ? list
                    : list.Select(i =>
                    {
                        if (i.fmask != null || !dic.TryGetValue(i.type, out var fmask))
                            return i;

                        return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
                    });

            result = result
                .OrderBy(x => x.start)
                .ToList();

            return result;
        }
    }
}
