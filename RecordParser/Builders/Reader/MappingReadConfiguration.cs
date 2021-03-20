using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    internal readonly struct MappingReadConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public int? length { get; }
        public Expression fmask { get; }
        public Type type { get; }

        public MappingReadConfiguration(MemberExpression prop, int start, int? length, Type type, Expression fmask)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.type = type;
            this.fmask = fmask;
        }

        public static IEnumerable<MappingReadConfiguration> Merge(
            IEnumerable<MappingReadConfiguration> list,
            IReadOnlyDictionary<Type, Expression> dic)
        {
            var result = dic.Any() != true
                    ? list
                    : list.Select((Func<MappingReadConfiguration, MappingReadConfiguration>)(i =>
                    {
                        if (i.fmask != null || !dic.TryGetValue(i.type, out var fmask))
                            return i;


/* Unmerged change from project 'RecordParser (netcoreapp2.1)'
Before:
                        return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
After:
                        return new Generic.MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
*/

/* Unmerged change from project 'RecordParser (netcoreapp3.1)'
Before:
                        return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
After:
                        return new Generic.MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
*/

/* Unmerged change from project 'RecordParser (netcoreapp5.0)'
Before:
                        return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
After:
                        return new Generic.MappingConfiguration(i.prop, i.start, i.length, i.type, fmask);
*/
                        return (MappingReadConfiguration)new MappingReadConfiguration(i.prop, i.start, i.length, i.type, fmask);
                    }));

            result = result
                .OrderBy(x => x.start)
                .ToList();

            return result;
        }
    }
}
