using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReaderBuilder<T>
    {
        IFixedLengthReader<T> Build();
        IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        IFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, Expression<Func<string, R>> convert = null, Expression<Func<string, bool>> skipRecordWhen = null);
    }

    public class FixedLengthReaderBuilder<T> : IFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingConfiguration(member, startIndex, length, typeof(R), convert, skipRecordWhen));
            return this;
        }

        public IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            dic.Add(typeof(R), ex);
            return this;
        }

        public IFixedLengthReader<T> Build()
        {
            var map = GenericRecordParser.Merge(list, dic);
            var func = StringExpressionParser.RecordParser<T>(map).Compile();

            return new FixedLengthReader<T>(map, func);
        }
    }
}
