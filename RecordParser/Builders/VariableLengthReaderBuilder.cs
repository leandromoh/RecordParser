using RecordParser.Generic;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReaderBuilder<T>
    {
        IVariableLengthReader<T> Build(string separator, CultureInfo cultureInfo = null);
        IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);
        IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanT<R> convert = null);
    }

    public class VariableLengthReaderBuilder<T> : IVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingConfiguration> list = new Dictionary<int, MappingConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body;
            var config = new MappingConfiguration(member, indexColumn, null, typeof(R), convert?.WrapInLambdaExpression());
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IVariableLengthReader<T> Build(string separator, CultureInfo cultureInfo = null)
        {
            var map = MappingConfiguration.Merge(list.Select(x => x.Value), dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map);

            func = CultureInfoVisitor.ReplaceCulture(func, cultureInfo);

            return new VariableLengthReader<T>(map, func.Compile(), separator);
        }
    }
}
