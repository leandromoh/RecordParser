using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public class FixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public FixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingConfiguration(member, startIndex, length, typeof(R), convert, skipRecordWhen));
            return this;
        }

        public FixedLengthReaderBuilder<T> DefaultConvert<R>(Expression<Func<string, R>> ex)
        {
            dic.Add(typeof(R), ex);
            return this;
        }

        public FixedLengthReader<T> Build() => new FixedLengthReader<T>(Merge());

        public List<MappingConfiguration> Merge()
        {
            if (dic?.Any() != true)
                return list;

            var result = list
                .Select(i =>
                {
                    var fmask = i.fmask ?? (dic.TryGetValue(i.type, out var ex) ? ex : null);
                    return new MappingConfiguration(i.prop, i.start, i.length, i.type, fmask, i.skipWhen);
                })
                .ToList();

            return result;
        }
    }
}
