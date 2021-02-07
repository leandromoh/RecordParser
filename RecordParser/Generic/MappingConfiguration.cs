using System;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    internal readonly struct MappingConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public int? length { get; }
        public Expression fmask { get; }
        public Type type { get; }

        public MappingConfiguration(MemberExpression prop, int start, int? length, Type type, Expression fmask)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.type = type;
            this.fmask = fmask;
        }
    }
}
