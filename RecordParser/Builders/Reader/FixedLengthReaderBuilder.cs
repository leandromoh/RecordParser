﻿using RecordParser.Generic;
using RecordParser.Visitors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReaderBuilder<T>
    {
        IFixedLengthReader<T> Build(CultureInfo cultureInfo = null);
        IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);
        IFixedLengthReaderBuilder<T> Map<R>(Expression<Func<T, R>> ex, int startIndex, int length, FuncSpanT<R> convert = null);
    }

    public class FixedLengthReaderBuilder<T> : IFixedLengthReaderBuilder<T>
    {
        private readonly List<MappingReadConfiguration> list = new List<MappingReadConfiguration>();
        private readonly Dictionary<Type, Expression> dic = new Dictionary<Type, Expression>();

        public IFixedLengthReaderBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int startIndex, int length,
            FuncSpanT<R> convert = null)
        {
            var member = ex.Body as MemberExpression ?? throw new ArgumentException("Must be member expression", nameof(ex));
            list.Add(new MappingReadConfiguration(member, startIndex, length, typeof(R), convert?.WrapInLambdaExpression()));
            return this;
        }

        public IFixedLengthReaderBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            dic.Add(typeof(R), ex?.WrapInLambdaExpression());
            return this;
        }

        public IFixedLengthReader<T> Build(CultureInfo cultureInfo = null) 
        {
            var map = MappingReadConfiguration.Merge(list, dic);
            var func = SpanExpressionParser.RecordParserSpan<T>(map);

            func = CultureInfoVisitor.ReplaceCulture(func, cultureInfo);

            return new FixedLengthReader<T>(map, func.Compile());
        }
    }
}
