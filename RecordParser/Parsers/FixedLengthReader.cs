using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface IFixedLengthReader<T>
    {
        T Parse(string line);
    }

    public interface ISpanFixedLengthReader<T>
    {
        T Parse(string line);
    }

    public class FixedLengthReader<T> : BaseFixedLengthReader<T>, IFixedLengthReader<T>
    {
        private readonly Func<string[], T> parser;

        internal FixedLengthReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
            : base(list) => this.parser = parser;

        public override T Parse(string line)
        {
            var csv = new string[config.Length];

            for (var i = 0; i < config.Length; i++)
                csv[i] = line.Substring(config[i].start, config[i].length);

            return parser(csv);
        }
    }

    public class SpanFixedLengthReader<T> : BaseFixedLengthReader<T>, ISpanFixedLengthReader<T>
    {
        private readonly Func<string, (int, int)[], T> parser;

        internal SpanFixedLengthReader(IEnumerable<MappingConfiguration> list, Func<string, (int, int)[], T> parser)
            : base(list) => this.parser = parser;

        public override T Parse(string line)
        {
            return parser(line, config);
        }
    }

    public abstract class BaseFixedLengthReader<T> 
    {
        protected readonly (int start, int length)[] config;

        protected BaseFixedLengthReader(IEnumerable<MappingConfiguration> list) =>
            config = list.Select(x => (x.start, x.length.Value)).ToArray();

        public abstract T Parse(string line);
    }
}
