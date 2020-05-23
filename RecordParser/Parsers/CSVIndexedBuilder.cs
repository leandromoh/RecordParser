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

        public CSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum, string mask = null)
        {
            list.Add(indexColum, new MappingConfiguration
            {
                prop = ex.Body as MemberExpression ?? throw new Exception("cu"),
                start = indexColum,
                type = typeof(R),
                mask = mask
            });

            return this;
        }

        public CSVReader<T> Build() => new CSVReader<T>(list.Select(x => x.Value));
    }
}
