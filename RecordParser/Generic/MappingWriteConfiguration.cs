using System;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public enum Padding
    {
        Left = 0,
        Right = 1,
    }

    public readonly struct MappingWriteConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public int? length { get; }

        // span, value, offset -> bool
        public Func<Expression, Expression, Expression, Expression> converter { get; }
        public string format { get; }
        public Type type { get; }
        public Padding padding { get; }
        public char paddingChar { get; }

        public MappingWriteConfiguration(MemberExpression prop, int start, int? length, Func<Expression, Expression, Expression, Expression> converter, string format, Padding padding, char paddingChar, Type type)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.converter = converter;
            this.format = format;
            this.type = type;
            this.padding = padding;
            this.paddingChar = paddingChar;
        }
    }
}
