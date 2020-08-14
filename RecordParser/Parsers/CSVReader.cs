using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ICSVReader<T>
    {
        T Parse(string str);
    }

    public interface ISpanCSVReader<T>
    {
        T Parse(string str);
    }

    public class CSVReader<T> : BaseCSVReader<T>, ICSVReader<T>
    {
        private readonly Func<string[], T> parser;

        internal CSVReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
            : base(list) => this.parser = parser;


        public override T Parse(string str)
        {
            var csv = GenericRecordParser.IndexedSplit(str, ";", config, nth, 
                (start, length) => str.Substring(start, length).Trim());

            return parser(csv);
        }
    }

    public class SpanCSVReader<T> : BaseCSVReader<T>, ISpanCSVReader<T>
    {
        private readonly Func<string, (int, int)[], T> parser;

        internal SpanCSVReader(IEnumerable<MappingConfiguration> list, Func<string, (int, int)[], T> parser)
            : base(list) => this.parser = parser;

        public override T Parse(string str)
        {
            var csv = GenericRecordParser.IndexedSplit(str, ";", config, nth, ValueTuple.Create);

            return parser(str, csv);
        }
    }

    public abstract class BaseCSVReader<T> 
    {
        protected readonly int[] config;
        protected readonly int nth;

        protected BaseCSVReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
        }

        public abstract T Parse(string str);
    }
}
