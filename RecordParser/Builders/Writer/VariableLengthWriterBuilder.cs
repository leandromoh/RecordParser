using RecordParser.Engines.Writer;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public interface IVariableLengthWriterBuilder<T>
    {
        IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null);
        IVariableLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex);

        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format);
        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanTIntBool<R> converter = null);
    }

    public class VariableLengthWriterBuilder<T> : IVariableLengthWriterBuilder<T>
    {
        private readonly Dictionary<int, MappingWriteConfiguration> list = new();
        private readonly Dictionary<Type, Func<Expression, Expression, Expression, Expression>> dic = new();

        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, FuncSpanTIntBool<R> converter = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, null, converter.WrapInLambdaExpression(), null, default, default, typeof(R));
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, null, null, format, default, default, typeof(R));
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthWriterBuilder<T> DefaultTypeConvert<R>(FuncSpanTIntBool<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IVariableLengthWriter<T> Build(string separator, CultureInfo cultureInfo = null)
        {
            var maps = MappingWriteConfiguration.Merge(list.Select(x => x.Value), dic);
            var expression = VariableLengthWriterEngine.GetFuncThatSetProperties<T>(maps, cultureInfo);

            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }
    }
}
