using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReaderBuilder<T> : Bla<IVariableLengthReaderBuilder<T>>
    {
        IVariableLengthReader<T> Build(string separator);
        IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanT<R> convert = null);
    }

    public interface Bla<T>
    {
        T DefaultTypeConvert<R>(FuncSpanT<R> ex);
    }

    public class VariableLengthReaderBuilder<T> : IVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingConfiguration(member, indexColumn, null, typeof(R), convert?.WrapInLambdaExpression());
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IVariableLengthReader<T> Build(string separator)
        {
            var map = GenericRecordParser.Merge(list.Select(x => x.Value), dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map).Compile();

            return new VariableLengthReader<T>(map, func, separator);
        }
    }
}
