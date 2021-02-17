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
        public string format { get; }
        public IFormatProvider formatProvider { get; }
        public Type type { get; }
        public Padding padding { get; }
        public char paddingChar { get; }

        public MappingWriteConfiguration(MemberExpression prop, int start, int? length, string format, Padding padding, char paddingChar, Type type, IFormatProvider formatProvider)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.format = format;
            this.type = type;
            this.formatProvider = formatProvider;
            this.padding = padding;
            this.paddingChar = paddingChar;
        }
    }
}
