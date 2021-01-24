﻿using System;
using System.Linq.Expressions;

namespace RecordParser.Parsers
{
    public interface IVariableLengthReaderSequentialBuilder<T>
    {
        IVariableLengthReader<T> Build(string separator);
        IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex);
        IVariableLengthReaderSequentialBuilder<T> Map<R>(Expression<Func<T, R>> ex, Expression<Func<string, R>> convert = null, Expression<Func<string, bool>> skipRecordWhen = null);
        IVariableLengthReaderSequentialBuilder<T> Skip(int collumCount);
    }

    public class VariableLengthReaderSequentialBuilder<T> : IVariableLengthReaderSequentialBuilder<T>
    {
        private readonly IVariableLengthReaderBuilder<T> indexed = new VariableLengthReaderBuilder<T>();
        private int currentIndex = -1;

        public IVariableLengthReaderSequentialBuilder<T> Map<R>(
            Expression<Func<T, R>> ex,
            Expression<Func<string, R>> convert = null,
            Expression<Func<string, bool>> skipRecordWhen = null)
        {
            indexed.Map(ex, ++currentIndex, convert, skipRecordWhen);
            return this;
        }

        public IVariableLengthReaderSequentialBuilder<T> Skip(int collumCount)
        {
            currentIndex += collumCount;
            return this;
        }

        public IVariableLengthReaderSequentialBuilder<T> DefaultTypeConvert<R>(Expression<Func<string, R>> ex)
        {
            indexed.DefaultTypeConvert(ex);
            return this;
        }

        public IVariableLengthReader<T> Build(string separator) => indexed.Build(separator);
    }
}
