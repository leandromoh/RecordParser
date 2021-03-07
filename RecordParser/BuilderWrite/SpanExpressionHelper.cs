﻿using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RecordParser.BuilderWrite
{
    internal static class SpanExpressionHelper
    {
        public static void DAs(Expression prop, MappingWriteConfiguration map, List<Expression> commands, ParameterExpression temp, ParameterExpression charsWritten, Expression gotoReturn, CultureInfo cultureInfo)
        {
            if (map.converter != null)
            {
                var toLarge = map.converter(temp, prop, charsWritten);
                commands.Add(Expression.IfThen(toLarge, gotoReturn));
                return;
            }

            if (prop.Type.IsEnum)
            {
                var result = Expression.Variable(typeof((bool, int)), "temp");

                var toLarge = Expression.Block(variables: new[] { result },
                    Expression.Assign(result, TryFormatEnum(prop, temp)),
                    Expression.Assign(charsWritten, Expression.PropertyOrField(result, "Item2")),
                    Expression.Not(Expression.PropertyOrField(result, "Item1")));

                commands.Add(Expression.IfThen(toLarge, gotoReturn));
                return;
            }

            if (prop.Type == typeof(string))
            {
                var strSpan = StringAsSpan(prop);

                var toLarge = Expression.GreaterThan(
                        Expression.PropertyOrField(prop, "Length"),
                        Expression.PropertyOrField(temp, "Length"));

                commands.Add(Expression.IfThen(toLarge, gotoReturn));

                commands.Add(
                    Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, temp));

                commands.Add(
                    Expression.Assign(charsWritten, Expression.PropertyOrField(prop, "Length")));
            }
            else
            {
                var format = map.format is null
                    ? Expression.Default(typeof(ReadOnlySpan<char>))
                    : StringAsSpan(Expression.Constant(map.format));

                var tryFormat =
                    Expression.Call(prop, "TryFormat", Type.EmptyTypes,
                    temp, charsWritten, format, Expression.Constant(cultureInfo, typeof(CultureInfo)));

                commands.Add(Expression.IfThen(Expression.Not(tryFormat), gotoReturn));
            }
        }

        private static readonly ConstructorInfo _boolIntTupleConstructor;

        static SpanExpressionHelper()
        {
            _boolIntTupleConstructor = typeof((bool, int)).GetConstructor(new[] { typeof(bool), typeof(int) });
        }

        private static NewExpression CreateTuple(bool success, Expression countWritten)
        {
            Debug.Assert(countWritten.Type == typeof(int));

            return Expression.New(_boolIntTupleConstructor, Expression.Constant(success), countWritten);
        }

        public static Expression GetReturn(bool success, Expression countWritten, LabelTarget returnTarget)
        {
            var returnValue = CreateTuple(success, countWritten);

            return Expression.Return(returnTarget, returnValue);
        }

        private static Expression TryFormatEnum(Expression enumValue, Expression span)
        {
            var type = enumValue.Type;

            Debug.Assert(type.IsEnum);
            Debug.Assert(span.Type == typeof(Span<char>));

            var under = Enum.GetUnderlyingType(type);
            var charsWritten = Expression.Variable(typeof(int), "charsWritten");

            var body = Enum.GetValues(type)
                .Cast<object>()
                .Select(color =>
                {
                    var text = color.ToString();

                    var valueEquals = Expression.Equal(enumValue, Expression.Constant(color, type));

                    var enoughSpace = Expression.GreaterThanOrEqual(
                        Expression.PropertyOrField(span, "Length"),
                        Expression.Constant(text.Length));

                    var strSpan = StringAsSpan(Expression.Constant(text));

                    var ifTrue = Expression.Block(
                        Expression.Call(strSpan, "CopyTo", Type.EmptyTypes, span),
                        Expression.Constant((true, text.Length)));

                    var ifFalse = Expression.Constant((false, 0));

                    var block = Expression.Condition(enoughSpace, ifTrue, ifFalse);

                    return (value: block, condition: valueEquals);
                })
                .Reverse()
                .Aggregate(Expression.Condition(
                        test: Expression.Call(
                               Expression.Convert(enumValue, under), "TryFormat", Type.EmptyTypes, span, charsWritten,
                                Expression.Default(typeof(ReadOnlySpan<char>)), Expression.Constant(null, typeof(CultureInfo))),
                        ifTrue: CreateTuple(true, charsWritten),
                        ifFalse: CreateTuple(false, charsWritten)),

                            (acc, item) =>
                                Expression.Condition(
                                    item.condition,
                                    item.value,
                                    acc));

            var blockExpr = Expression.Block(
                                variables: new[] { charsWritten },
                                expressions: body);

            return blockExpr;
        }

        public static Expression StringAsSpan(Expression str) =>
            Expression.Call(typeof(MemoryExtensions), "AsSpan", Type.EmptyTypes, str);

        public static Expression Slice(Expression span, int start, int length) =>
            Slice(span, start, Expression.Constant(length));

        public static Expression Slice(Expression span, int start, Expression length) =>
            Expression.Call(span, "Slice", Type.EmptyTypes, Expression.Constant(start), length);

        public static Span<char> PadRight(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total);

            for (int i = span.Length, totalWidth = total.Length; i < totalWidth; i++)
                total[i] = paddingChar;

            return total;
        }

        public static Span<char> PadLeft(Span<char> span, Span<char> total, char paddingChar)
        {
            var diff = total.Length - span.Length;
            if (diff <= 0) return span;

            span.CopyTo(total.Slice(diff));

            for (var i = 0; i < diff; i++)
                total[i] = paddingChar;

            return total;
        }
    }
}