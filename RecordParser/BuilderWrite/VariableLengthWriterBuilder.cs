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

            commands.Add(Expression.Assign(position, Expression.Constant(0)));

            var blocks = new List<BlockExpression>(mappedColumns.Count());
            var i = -1;
            foreach (var map in mappedColumns)
            {
            reloop:

                var blockCommands = new List<Expression>();

                blockCommands.Add(
                    Expression.Assign(spanTemp, Expression.Call(span, "Slice", Type.EmptyTypes, position)));

                if (++i != map.start)
                {
                    var condition = Expression.LessThanOrEqual(
                        delimiterLength,
                        Expression.PropertyOrField(spanTemp, "Length"));

                    var ifTrue = Expression.Block(
                        Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, spanTemp),
                        Expression.AddAssign(position, delimiterLength),
                        Expression.Constant(true));

                    blockCommands.Add(Expression.Condition(condition, ifTrue, Expression.Constant(false)));

                    blocks.Add(Expression.Block(blockCommands));

                    goto reloop;
                }

                var prop = replacer.Visit(map.prop);

                if (prop.Type.IsEnum)
                {
                    prop = Expression.Call(prop, "ToString", Type.EmptyTypes);
                }

                if (prop.Type == typeof(string))
                {
                    var strSpan = StringAsSpan(prop);

                    var condition = Expression.LessThanOrEqual(
                        Expression.PropertyOrField(prop, "Length"),
                        Expression.PropertyOrField(spanTemp, "Length"));

                    var ifTrue = Expression.Block(
                        Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, spanTemp),
                        Expression.Assign(offset, Expression.PropertyOrField(prop, "Length")),
                        Expression.Constant(true));

                    blockCommands.Add(Expression.Condition(condition, ifTrue, Expression.Constant(false)));
                }
                else
                {
                    var format = map.format is null
                        ? Expression.Default(typeof(ReadOnlySpan<char>))
                        : StringAsSpan(Expression.Constant(map.format));

                    var condition =
                        Expression.Call(prop, "TryFormat", Type.EmptyTypes,
                        spanTemp, offset, format, Expression.Constant(map.formatProvider, typeof(IFormatProvider)));

                    blockCommands.Add(condition);
                }

                blocks.Add(Expression.Block(blockCommands));

                blocks.Add(Expression.Block(new Expression[]
                {
                    Expression.Assign(spanTemp, Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset)),

                    Expression.Condition(
                        test: Expression.LessThanOrEqual(
                              delimiterLength,
                              Expression.PropertyOrField(spanTemp, "Length")),

                    ifTrue: Expression.Block(
                            Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, spanTemp),
                            Expression.AddAssign(position, Expression.Add(offset, delimiterLength)),
                            Expression.Constant(true)),

                    ifFalse: Expression.Constant(false))
                }));
            }

            //remove the last block (contains copy delimiter and add delimiterLength to position)
            blocks.RemoveAt(blocks.Count - 1);

            var testAnds = blocks.Aggregate<Expression>((acc, item) => Expression.AndAlso(acc, item));
            commands.Add(Expression.Condition(testAnds,
                         Expression.Add(position, offset),
                         Expression.Constant(0)));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanTInt<T>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;
        }
    }
}
