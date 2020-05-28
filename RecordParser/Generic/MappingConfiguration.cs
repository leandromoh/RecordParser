using System;
using System.Linq.Expressions;

namespace RecordParser.Generic
{
    public class MappingConfiguration
    {
        public MemberExpression prop { get; }
        public int start { get; }
        public int? length { get; }
        public Expression fmask { get; }
        public Expression skipWhen { get; }
        public Type type { get; }

        public MappingConfiguration(MemberExpression prop, int start, int? length, Type type, Expression fmask, Expression skipWhen)
        {
            this.prop = prop;
            this.start = start;
            this.length = length;
            this.type = type;
            this.fmask = fmask;
            this.skipWhen = skipWhen;
        }
    }
}
