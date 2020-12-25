﻿using RecordParser.Generic;
using System;
using System.Buffers;
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
        private readonly string delimiter;

        internal CSVReader(IEnumerable<MappingConfiguration> list, Func<string[], T> parser)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            this.parser = parser;
            delimiter = ";";
        }

        public T Parse(string str)
        {
            string[] csv = IndexOfNth(str, delimiter, config, nth + 1);
            T result = parser(csv);
            return result;
        }

        private static string[] IndexOfNth(string span, string delimiter, int[] config, int size)
        {
            var csv = new string[config.Length];
            var scanned = -1;
            var position = 0;
            var j = 0;

            for (var i = 0; i < size && j < config.Length; i++)
            {
                var (startIndex, length) = ParseChunk(ref span, ref scanned, ref position, delimiter);

                if (i == config[j])
                {
                    csv[j] = span.Substring(startIndex, length).Trim();
                    j++;
                }
            }

            return csv;
        }

        private static (int, int) ParseChunk(ref string span, ref int scanned, ref int position, string delimiter)
        {
            scanned += position + 1;

            position = span.IndexOf(delimiter, scanned) - scanned;
            if (position < 0)
            {
                position = span.Length - scanned;
            }

            return (scanned, position);
        }
    }
}
