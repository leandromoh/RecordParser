using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public class FixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();

        public FixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, string mask = null)
        {
            list.Add(new MappingConfiguration
            {
                prop = ex.Body as MemberExpression ?? throw new Exception("cu"),
                start = startIndex,
                length = length,
                type = typeof(R),
                mask = mask
            });

            return this;
        }

        public FixedLengthReader<T> Build() => new FixedLengthReader<T>(list);
    }
}
