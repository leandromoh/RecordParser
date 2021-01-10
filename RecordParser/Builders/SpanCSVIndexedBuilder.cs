using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ISpanCSVIndexedBuilder<T>
    {
        ISpanCSVReader<T> Build();
        ISpanCSVIndexedBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex);
        ISpanCSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum, Expression<Func<ReadOnlySpanChar, R>> convert = null, Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null);
    }

    public class SpanCSVIndexedBuilder<T> : ISpanCSVIndexedBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();
        private readonly ReadOnlySpanVisitor visitor = new ReadOnlySpanVisitor();

        public ISpanCSVIndexedBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColum,
            Expression<Func<ReadOnlySpanChar, R>> convert = null,
            Expression<Func<ReadOnlySpanChar, bool>> skipRecordWhen = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColum, null, typeof(R), visitor.Modify(convert), visitor.Modify(skipRecordWhen));
            list.Add(indexColum, config);
            
            return this;
        }

        public ISpanCSVIndexedBuilder<T> DefaultTypeConvert<R>(Expression<Func<ReadOnlySpanChar, R>> ex)
        {
            dic.Add(typeof(R), visitor.Modify(ex));
            
            return this;
        }

        public ISpanCSVReader<T> Build()
        {
            var map = GenericRecordParser.Merge(list.Select(x => x.Value), dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map).Compile();

            return new SpanCSVReader<T>(map, func);
        }
    }
}
