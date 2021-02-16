using System;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public readonly struct MappingWriteConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public string format { get; }
        public IFormatProvider formatProvider { get; }
        public Type type { get; }

        public MappingWriteConfiguration(MemberExpression prop, int start, string format, Type type, IFormatProvider formatProvider)
        {
            this.prop = prop;
            this.start = start;
            this.format = format;
            this.type = type;
            this.formatProvider = formatProvider;
        }
    }
}
