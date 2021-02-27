using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReaderBuilder<T> : Bla<IFixedLengthReaderBuilder<T>>
    {
        IFixedLengthReader<T> Build();
        IFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanT<R> convert = null);
    }

    public class FixedLengthReaderBuilder<T> : IFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingConfiguration> list = new List<MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingConfiguration(member, startIndex, length, typeof(R), convert?.WrapInLambdaExpression()));
            return this;
        }

        public IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IFixedLengthReader<T> Build() 
        {
            var map = GenericRecordParser.Merge(list, dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map).Compile();

            return new FixedLengthReader<T>(map, func);
        }
    }
}
