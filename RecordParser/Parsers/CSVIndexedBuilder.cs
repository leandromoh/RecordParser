using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public class CSVIndexedBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();

        public CSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColum, null, typeof(R), convert, skipRecordWhen);
            list.Add(indexColum, config);
            return this;
        }

        public CSVReader<T> Build() => new CSVReader<T>(list.Select(x => x.Value));
    }
}
