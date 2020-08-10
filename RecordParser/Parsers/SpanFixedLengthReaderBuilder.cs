using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ISpanFixedLengthReaderBuilder<T>
    {
        ISpanFixedLengthReader<T> Build();
        ISpanFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex);
        ISpanFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, Expression<Func<ReadOnlySpanChar, R>> convert = null, Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null);
    }

    public class SpanFixedLengthReaderBuilder<T> : ISpanFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();
        private readonly ReadOnlySpanVisitor visitor = new ReadOnlySpanVisitor();

        public ISpanFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            Expression<Func<ReadOnlySpanChar, R>> convert = null,
            Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingConfiguration(member, startIndex, length, typeof(R), visitor.Modify(convert), skipRecordWhen));
            return this;
        }

        public ISpanFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex)
        {
            dic.Add(typeof(R), visitor.Modify(ex));
            return this;
        }

        public ISpanFixedLengthReader<T> Build() =>
            new SpanFixedLengthReader<T>(GenericRecordParser.Merge(list, dic));
    }

}
