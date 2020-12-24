﻿using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public interface ICSVReader<T>
    {
        T Parse(string str);
    }

    public class CSVReader<T> : ICSVReader<T>
    {
        private readonly Func<string[], T> parser;
        private readonly int[] config;
        private readonly int nth;

        internal CSVReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
        }

        public T Parse(string str)
        {
            var csv = GenericRecordParser.IndexedSplit(str, ";", config, nth, 
                (start, length) => str.Substring(start, length).Trim());

            return parser(csv);
        }
    }
}
