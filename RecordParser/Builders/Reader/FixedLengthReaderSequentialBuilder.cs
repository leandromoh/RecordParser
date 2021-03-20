﻿using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace RecordParser.Builders.Reader
{
    public interface IFixedLengthReaderSequentialBuilder<T>
    {
        IFixedLengthReader<T> Build(CultureInfo cultureInfo = null);
        IFixedLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex);
        IFixedLengthReaderSequentialBuilder<T> Skip(int length);
        IFixedLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, int length, FuncSpanT<R> convert = null);
    }

    public class FixedLengthReaderSequentialBuilder<T> : IFixedLengthReaderSequentialBuilder<T>
    {
        private readonly IFixedLengthReaderBuilder<T> indexed = new FixedLengthReaderBuilder<T>();
        private int currentPosition = 0;

        public IFixedLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex, int length,
            FuncSpanT<R> convert = null)
        {
            indexed.Map(ex, currentPosition, length, convert);
            currentPosition += length;
            return this;
        }

        public IFixedLengthReaderSequentialBuilder<T> Skip(int length)
        {
            currentPosition += length;
            return this;
        }

        public IFixedLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(FuncSpanT<R> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public IFixedLengthReader<T> Build(CultureInfo cultureInfo = null) 
            => indexed.Build(cultureInfo);
    }
}
