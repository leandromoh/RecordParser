using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.BuilderWrite.SpanExpressionHelper;

namespace RecordParser.BuilderWrite
{
    public interface IVariableLengthWriterBuilder<T>
    {
        IVariableLengthWriter<T> Build(string separator);
        IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format = null);
    }

    public class VariableLengthWriterBuilder<T> : IVariableLengthWriterBuilder<T>
    {
        private readonly Dictionary<int, MappingWriteConfiguration> list = new Dictionary<int, MappingWriteConfiguration>();

        public IVariableLengthWriterBuilder<T> Map<R>(Expression<Func<T, R>> ex, int indexColumn, string format = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            var config = new MappingWriteConfiguration(member, indexColumn, null, format, default, default, typeof(R), null);
            list.Add(indexColumn, config);
            return this;
        }

        public IVariableLengthWriter<T> Build(string separator)
        {
            var maps = list.Select(x => x.Value).OrderBy(x => x.start);
            var expression = GetFuncThatSetProperties(maps);
            
            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }

        private static Expression<FuncSpanSpanTInt<T>> GetFuncThatSetProperties(IEnumerable<MappingWriteConfiguration> mappedColumns)
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
                reloop:
                
                commands.Add(
                    Expression.Assign(spanTemp, Expression.Call(span, "Slice", Type.EmptyTypes, position)));

                if (++i != map.start)
                {
                    commands.Add(
                        Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, spanTemp));

                    commands.Add(
                        Expression.AddAssign(position, delimiterLength));

                    goto reloop;
                }

                var prop = replacer.Visit(map.prop);

                DAs(prop, map, commands, spanTemp, offset);

                commands.Add(
                    Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset)));

                commands.Add(
                    Expression.AddAssign(position, Expression.Add(offset, delimiterLength)));
            }

            //remove the last 2 commands (copy delimiter and add delimiterLength to position)
            commands.RemoveRange(commands.Count - 2, 2);

            commands.Add(Expression.Add(position, offset));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanTInt<T>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;

            Expression StringAsSpan(Expression str) => Expression.Call(typeof(MemoryExtensions), "AsSpan", Type.EmptyTypes, str);
        }
    }
}
