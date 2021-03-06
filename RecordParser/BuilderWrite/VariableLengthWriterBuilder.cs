using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.BuilderWrite.SpanExpressionHelper;

namespace RecordParser.BuilderWrite
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
            var expression = GetFuncThatSetProperties(maps, cultureInfo);

            return new VariableLengthWriter<T>(expression.Compile(), separator);
        }

        private static Expression<FuncSpanSpanTInt<T>> GetFuncThatSetProperties(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
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

            LabelTarget returnTarget = Expression.Label(typeof(int));
            GotoExpression returnPosition = Expression.Return(returnTarget, position);
            GotoExpression returnPositionOffset = Expression.Return(returnTarget, Expression.Add(position, offset));

            var i = -1;
            foreach (var map in mappedColumns)
            {
                reloop:

                commands.Add(
                    Expression.Assign(spanTemp, Expression.Call(span, "Slice", Type.EmptyTypes, position)));

                var isEmptyColumn = ++i != map.start;
                if (isEmptyColumn)
                {
                    WriteDelimiter(spanTemp, delimiterLength, returnPosition);
                    goto reloop;
                }

                var prop = replacer.Visit(map.prop);

                var gotoReturn = map.converter == null && (prop.Type.IsEnum || prop.Type == typeof(string))
                    ? returnPosition
                    : returnPositionOffset;

                DAs(prop, map, commands, spanTemp, offset, gotoReturn, cultureInfo);

                WriteDelimiter(
                    Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset),
                    Expression.Add(offset, delimiterLength),
                    returnPositionOffset);
            }

            //remove the last 3 commands (if toLarge, copy delimiter and add delimiterLength to position)
            commands.RemoveRange(commands.Count - 3, 3);

            commands.Add(Expression.AddAssign(position, offset));
            commands.Add(Expression.Return(returnTarget, position));


            commands.Add(Expression.Label(returnTarget, Expression.Constant(0)));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanTInt<T>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;

            void WriteDelimiter(Expression freeSpan, Expression addToPosition, GotoExpression returnExpression)
            {
                var toLarge = Expression.GreaterThan(
                    delimiterLength,
                    Expression.PropertyOrField(freeSpan, "Length"));

                commands.Add(Expression.IfThen(toLarge, returnExpression));

                commands.Add(
                    Expression.Call(delimiter, "CopyTo", Type.EmptyTypes, freeSpan));

                commands.Add(
                    Expression.AddAssign(position, addToPosition));
            }
        }
    }
}
