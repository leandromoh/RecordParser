using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using RecordParser.BuilderWrite;
using static RecordParser.BuilderWrite.WriteEngine;
using static RecordParser.Engines.ExpressionHelper;

namespace RecordParser.Engines.Writer
{
    internal static class FixedLengthWriterEngine
    {
        public static Expression<FuncSpanTInt<T>> GetFuncThatSetProperties<T>(IEnumerable<MappingWriteConfiguration> mappedColumns, CultureInfo cultureInfo)
        {
            // parameters
            ParameterExpression span = Expression.Variable(typeof(Span<char>), "span");
            ParameterExpression inst = Expression.Variable(typeof(T), "inst");

            var replacer = new ParameterReplacerVisitor(inst);

            // variables
            ParameterExpression offset = Expression.Variable(typeof(int), "charsWritten");
            ParameterExpression temp = Expression.Variable(typeof(Span<char>), "tempSpan");

            List<ParameterExpression> variables = new List<ParameterExpression>();
            variables.Add(offset);
            variables.Add(temp);

            // commands
            List<Expression> commands = new List<Expression>();

            LabelTarget returnTarget = Expression.Label(typeof((bool, int)));

            var necessarySpace = Expression.Constant(mappedColumns.Max(x => x.start + x.length.Value));

            var tooShort = Expression.LessThan(
                                Expression.PropertyOrField(span, "Length"),
                                necessarySpace);

            var charsWritten = Expression.Constant(0);

            commands.Add(Expression.IfThen(tooShort, GetReturn(false, charsWritten, returnTarget)));

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

            var lambda = Expression.Lambda<FuncSpanTInt<T>>(blockExpr, new[] { span, inst });

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
