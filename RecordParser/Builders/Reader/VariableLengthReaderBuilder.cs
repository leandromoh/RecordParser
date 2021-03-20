using RecordParser.Engines.Reader;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IVariableLengthReaderBuilder<T>
    {
        IVariableLengthReader<T> Build(string separator, CultureInfo cultureInfo = null);
        IVariableLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);
        IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanT<R> convert = null);
    }

    public class VariableLengthReaderBuilder<T> : IVariableLengthReaderBuilder<T>
    {
        private readonly Dictionary<int, MappingReadConfiguration> list = new Dictionary<int, MappingReadConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IVariableLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingReadConfiguration(member, indexColumn, null, typeof(R), convert?.WrapInLambdaExpression());
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
            var map = MappingReadConfiguration.Merge(list.Select(x => x.Value), dic);
            var func = ReaderEngine.RecordParserSpan<T>(map);

            func = CultureInfoVisitor.ReplaceCulture(func, cultureInfo);

            return new VariableLengthReader<T>(map, func.Compile(), separator);
        }
    }
}
