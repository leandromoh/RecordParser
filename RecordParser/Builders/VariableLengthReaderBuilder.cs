using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReaderBuilder<T>
    {
        IVariableLengthReader<T> Build(string separator);
        IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum, Expression<Func<string, R>> convert = null, Expression<Func<string, bool>> skipRecordWhen = null);
    }

    public class VariableLengthReaderBuilder<T> : IVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColum, null, typeof(R), convert, skipRecordWhen);
            list.Add(indexColum, config);
            return this;
        }

        public IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            dic.Add(typeof(R), ex);
            return this;
        }

        public IVariableLengthReader<T> Build(string separator)
        {
            var map = GenericRecordParser.Merge(list.Select(x => x.Value), dic);
            var func = StringExpressionParser.RecordParser<T>(map).Compile();

            return new VariableLengthReader<T>(map, func, separator);
        }
    }
}
