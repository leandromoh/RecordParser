using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using static RecordParser.Engines.ExpressionHelper;
using static RecordParser.Engines.Writer.WriteEngine;

namespace RecordParser.Engines.Writer
{
    internal static class FixedLengthWriterEngine
    {
        public static Expression<FuncSpanTIntBool<T>> GetFuncThatSetProperties<T>(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
        {
            // parameters
            var span = Expression.Parameter(typeof(Span<char>), "span");
            var inst = Expression.Parameter(typeof(T), "inst");

            // variables
            var offset = Expression.Variable(typeof(int), "charsWritten");
            var temp = Expression.Variable(typeof(Span<char>), "tempSpan");

            var variables = new List<ParameterExpression>() { offset, temp };

            // commands
            var commands = new List<Expression>();
            var returnTarget = Expression.Label(typeof((bool, int)));
            var necessarySpace = Expression.Constant(mappedColumns.Max(x => x.start + x.length.Value));
            var charsWritten = Expression.Constant(0);
            var tooShort = Expression.LessThan(
                                Expression.PropertyOrField(span, "Length"),
                                necessarySpace);

            commands.Add(Expression.IfThen(tooShort, GetReturn(false, charsWritten, returnTarget)));

            var replacer = new ParameterReplacerVisitor(inst);

            foreach (var map in mappedColumns)
            {
                commands.Add(
                    Expression.Assign(temp, Slice(span, map.start, map.length.Value)));

                var prop = replacer.Visit(map.prop);

                var gotoReturn = map.UseTryPattern
                    ? GetReturn(false, Expression.Add(charsWritten, offset), returnTarget)
                    : GetReturn(false, charsWritten, returnTarget);

                var parse = DAs(prop, map, temp, offset, gotoReturn, cultureInfo);

                commands.Add(parse);

                CallPad(map);

                charsWritten = Expression.Constant(map.start + map.length.Value);
            }

            commands.Add(GetReturn(true, necessarySpace, returnTarget));

            commands.Add(Expression.Label(returnTarget, Expression.Constant(default((bool, int)))));

            var blockExpr = Expression.Block(variables, commands);

            var lambda = Expression.Lambda<FuncSpanTIntBool<T>>(blockExpr, new[] { span, inst });

            return lambda;

            void CallPad(MappingWriteConfiguration map)
            {
                var padFunc = map.padding == Padding.Left
                    ? nameof(FixedLengthPadWriter.PadLeft)
                    : nameof(FixedLengthPadWriter.PadRight);

                commands.Add(
                    Expression.Call(typeof(FixedLengthPadWriter), padFunc, Type.EmptyTypes,
                        Slice(temp, 0, offset),
                        temp,
                        Expression.Constant(map.paddingChar)));
            }
        }
    }
}
