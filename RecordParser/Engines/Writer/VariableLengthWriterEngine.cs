using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using static RecordParser.Engines.Writer.WriteEngine;

namespace RecordParser.Engines.Writer
{
    internal static class VariableLengthWriterEngine
    {
        public static Expression<FuncSpanSpanTInt<T>> GetFuncThatSetProperties<T>(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
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

            LabelTarget returnTarget = Expression.Label(typeof((bool, int)));
            Expression returnPosition = GetReturn(false, position, returnTarget);
            Expression returnPositionOffset = GetReturn(false, Expression.Add(position, offset), returnTarget);

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

                var gotoReturn = map.UseTryPattern
                    ? returnPositionOffset
                    : returnPosition;

                var parse = DAs(prop, map, spanTemp, offset, gotoReturn, cultureInfo);

                commands.Add(parse);

                WriteDelimiter(
                    Expression.Call(spanTemp, "Slice", Type.EmptyTypes, offset),
                    Expression.Add(offset, delimiterLength),
                    returnPositionOffset);
            }

            //remove the last 3 commands (if toLarge, copy delimiter and add delimiterLength to position)
            commands.RemoveRange(commands.Count - 3, 3);

            commands.Add(Expression.AddAssign(position, offset));
            commands.Add(GetReturn(true, position, returnTarget));


            commands.Add(Expression.Label(returnTarget, Expression.Constant(default((bool, int)))));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanSpanTInt<T>>(blockExpr, new[] { span, delimiter, inst });

            return lambda;

            void WriteDelimiter(Expression freeSpan, Expression addToPosition, Expression returnExpression)
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
