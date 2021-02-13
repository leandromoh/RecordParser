using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface ISpanFixedLengthReaderBuilder<T>
    {
        ISpanFixedLengthReader<T> Build();
        ISpanFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);
        ISpanFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanT<R> convert = null);
    }

    public class SpanFixedLengthReaderBuilder<T> : ISpanFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public ISpanFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingConfiguration(member, startIndex, length, typeof(R), convert?.WrapInLambdaExpression()));
            return this;
        }

        public ISpanFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public ISpanFixedLengthReader<T> Build() 
        {
            var map = GenericRecordParser.Merge(list, dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map).Compile();

            return new SpanFixedLengthReader<T>(map, func);
        }
    }

}
