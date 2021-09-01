using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RecordParser.Builders.Writer
{
    public enum Padding
    {
        /// <summary>
        /// Pads a text with leading characters to a specified total length.
        /// </summary>
        Left = 0,

        /// <summary>
        /// Pads a text with trailing characters to a specified total length.
        /// </summary>
        Right = 1,
    }

    internal readonly struct MappingWriteConfiguration
    {
        public Expression prop { get; }
        public int start { get; }
        public int? length { get; }

        public Delegate converter { get; }
        public string format { get; }
        public Type type { get; }
        public Padding padding { get; }
        public char paddingChar { get; }

        public bool UseTryPattern => converter != null || prop.Type != typeof(string);

        public MappingWriteConfiguration(Expression prop, int start, int? length, Delegate converter, string format, Padding padding, char paddingChar, Type type)
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

        public static IEnumerable<MappingWriteConfiguration> Merge(
            IEnumerable<MappingWriteConfiguration> list,
            IReadOnlyDictionary<Type, Delegate> dic)
        {
            var result = dic.Count is 0
                    ? list
                    : list.Select(i =>
                    {
                        if (i.converter != null || i.format != null || !dic.TryGetValue(i.type, out var fmask))
                            return i;

                        return new MappingWriteConfiguration(i.prop, i.start, i.length, fmask, i.format, i.padding, i.paddingChar, i.type);
                    });

            result = result
                .OrderBy(x => x.start)
                .ToList();

            return result;
        }
    }
}
