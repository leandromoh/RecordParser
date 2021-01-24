using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ISpanVariableLengthReaderBuilder<T>
    {
        ISpanVariableLengthReader<T> Build(string separator);
        ISpanVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex);
        ISpanVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum, Expression<Func<ReadOnlySpanChar, R>> convert = null, Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null);
    }

    public class SpanVariableLengthReaderBuilder<T> : ISpanVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();
        private readonly ReadOnlySpanVisitor visitor = new ReadOnlySpanVisitor();

        public ISpanVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum,
            Expression<Func<ReadOnlySpanChar, R>> convert = null,
            Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColum, null, typeof(R), visitor.Modify(convert), visitor.Modify(skipRecordWhen));
            list.Add(indexColum, config);
            
            return this;
        }

        public ISpanVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex)
        {
            dic.Add(typeof(R), visitor.Modify(ex));
            
            return this;
        }

        public ISpanVariableLengthReader<T> Build(string separator)
        {
            var map = GenericRecordParser.Merge(list.Select(x => x.Value), dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map).Compile();

            return new SpanVariableLengthReader<T>(map, func, separator);
        }
    }
}
