using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ICSVIndexedBuilder<T>
    {
        ICSVReader<T> Build();
        ICSVIndexedBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        ICSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum, Expression<Func<string, R>> convert = null, Expression<Func<string, bool>> skipRecordWhen = null);
    }

    public class CSVIndexedBuilder<T> : ICSVIndexedBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public ICSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColum, null, typeof(R), convert, skipRecordWhen);
            list.Add(indexColum, config);
            return this;
        }

        public ICSVIndexedBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            dic.Add(typeof(R), ex);
            return this;
        }

        public ICSVReader<T> Build()
        {
            var map = GenericRecordParser.Merge(list.Select(x => x.Value), dic);
            var func = StringExpressionParser.RecordParser<T>(map).Compile();

            return new CSVReader<T>(map, func);
        }
    }
}
