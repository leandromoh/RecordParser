using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using static RecordParser.Engines.Writer.WriteEngine;
using static RecordParser.Engines.ExpressionHelper;

namespace RecordParser.Engines.Writer
{
    internal static class VariableLengthWriterEngine
    {
        public static Expression<FuncSpanSpanTInt<T>> GetFuncThatSetProperties<T>(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
        {
            // parameters
            var span = Expression.Parameter(typeof(Span<char>), "span");
            var delimiter = Expression.Parameter(typeof(ReadOnlySpan<char>), "delimiter");
            var inst = Expression.Parameter(typeof(T), "inst");

            // variables
            var offset = Expression.Variable(typeof(int), "offset");
            var position = Expression.Variable(typeof(int), "position");
            var delimiterLength = Expression.Variable(typeof(int), "delimiterLength");
            var spanTemp = Expression.Variable(typeof(Span<char>), "spanTemp");

            var variables = new List<ParameterExpression>() { offset, position, delimiterLength, spanTemp };

            // commands
            var commands = new List<Expression>()
            {
                Expression.Assign(delimiterLength, Expression.PropertyOrField(delimiter, "Length")),
                Expression.Assign(position, Expression.Constant(0))
            };

            var returnTarget = Expression.Label(typeof((bool, int)));
            var returnPosition = GetReturn(false, position, returnTarget);
            var returnPositionOffset = GetReturn(false, Expression.Add(position, offset), returnTarget);

            var replacer = new ParameterReplacerVisitor(inst);

            var i = -1;
            foreach (var map in mappedColumns)
            {
                reloop:

                commands.Add(
                    Expression.Assign(spanTemp, Slice(span, position)));

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
                    Slice(spanTemp, offset),
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
