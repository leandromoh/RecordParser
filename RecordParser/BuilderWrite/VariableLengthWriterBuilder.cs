using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.BuilderWrite
{
    public interface IVariableLengthWriterBuilder<T>
    {
        IVariableLengthWriter<T> Build(string separator);
        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format = null, IFormatProvider formatProvider = null);
    }

    public class VariableLengthWriterBuilder<T> : IVariableLengthWriterBuilder<T>
    {
        private readonly Dictionary<int, MappingWriteConfiguration> list = new Dictionary<int, MappingWriteConfiguration>();
        private FuncSpanSpanIntT<T> func;

        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format = null, IFormatProvider formatProvider = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, format, typeof(R), formatProvider);
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthWriter<T> Build(string separator)
        {
            var maps = list.Select(x => x.Value).OrderBy(x => x.start);
            var expression = GetFuncThatSetProperties(maps);
            
            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }

        private static Expression<FuncSpanSpanIntT<T>> GetFuncThatSetProperties(IEnumerable<MappingWriteConfiguration> mappedColumns)
        {
            // parameters
            ParameterExpression span = Expression.Variable(typeof(Span<char>), "span");
            ParameterExpression delimiter = Expression.Variable(typeof(ReadOnlySpan<char>), "delimiter");
            ParameterExpression inst = Expression.Variable(typeof(T), "inst");

            var replacer = new ParameterReplacerVisitor(inst);

            // variables
            ParameterExpression offset = Expression.Variable(typeof(int), "offset");
            ParameterExpression position = Expression.Variable(typeof(int), "position");
            ParameterExpression delimiterLength = Expression.Variable(typeof(int), "delimiterLength");
            ParameterExpression spanTemp = Expression.Variable(typeof(Span<char>), "spanTemp");

            List<ParameterExpression> variables = new List<ParameterExpression>();
            variables.Add(offset);
            variables.Add(position);
            variables.Add(delimiterLength);
            variables.Add(spanTemp);

            // commands
            List<Expression> commands = new List<Expression>();
            commands.Add(
                Expression.Assign(delimiterLength, Expression.PropertyOrField(delimiter, "Length")));

            commands.Add(
                Expression.Assign(
                    offset,
                    Expression.Subtract(
                        Expression.Constant(0),
                        delimiterLength)));

            commands.Add(Expression.Assign(position, Expression.Constant(0)));

            var i = -1;
            foreach (var map in mappedColumns)
            {
                if (++i != map.start)
                {
                    continue;
                }

                var prop = replacer.Visit(map.prop);

                commands.Add(
                    Expression.Assign(spanTemp, Expression.Call(span, "Slice", Type.EmptyTypes, position)));
                
                if (prop.Type.IsEnum)
                {
                    prop = Expression.Call(prop, "ToString", Type.EmptyTypes);
                }

                if (prop.Type == typeof(string))
                {
                    var strSpan = StringAsSpan(prop);

                    commands.Add(
                        Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, spanTemp));

                    commands.Add(
                        Expression.Assign(offset, Expression.PropertyOrField(prop, "Length")));
                }
                else
                {
                    var format = map.format is null
                        ? Expression.Default(typeof(ReadOnlySpan<char>))
                        : StringAsSpan(Expression.Constant(map.format));

                    commands.Add(
                        Expression.Call(prop, "TryFormat", Type.EmptyTypes,
                        spanTemp, offset, format, Expression.Constant(map.formatProvider, typeof(IFormatProvider))));
                }

                commands.Add(
                    Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset)));

                commands.Add(
                    Expression.AddAssign(position, Expression.Add(offset, delimiterLength)));

            }

            commands.Add(Expression.Subtract(position, delimiterLength));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanIntT<T>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;

            Expression StringAsSpan(Expression str) => Expression.Call(typeof(MemoryExtensions), nameof(MemoryExtensions.AsSpan), Type.EmptyTypes, str);
        }
    }
}
