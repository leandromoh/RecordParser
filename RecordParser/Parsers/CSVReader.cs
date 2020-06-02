using RecordParser.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RecordParser.Parsers
{
    public class CSVReader<T>
    {
        public readonly GenericRecordParser<T> parser;
        private readonly int[] config;
        private readonly int nth;

        public CSVReader(IEnumerable<MappingConfiguration> list)
        {
            config = list.Select(x => x.start).ToArray();
            nth = config.Max();
            parser = new GenericRecordParser<T>(list);
        }

        public T Parse(string str)
        {
            var csv = IndexedSplit(str, ";", config, nth);

            return parser.Parse(csv);
        }

        /// <summary>
        /// Similar to String.Split, but receives what indexes matters,
        /// so does not allocates unwated occurrences.
        /// Equivalent to:
        /// var occurrences = text.split(separator);
        /// var matters = indexes.Select(i => occurrences[i])
        /// </summary>
        /// <param name="str"></param>
        /// <param name="delimiter"></param>
        /// <param name="config"></param>
        /// <param name="nth"></param>
        /// <returns></returns>
        public static string[] IndexedSplit(string str, string delimiter, int[] config, int nth)
        {
            Span<int> positions = stackalloc int[nth + 2];

            var i = 0;
            foreach (var x in IndexOfNth(str, delimiter, nth + 2))
            {
                positions[i++] = x;
            }

            if (i < positions.Length)
                throw new InvalidOperationException("menos colunas do q devia");

            i = 0;
            var csv = new string[config.Length];
            foreach (var index in config)
            {
                var start = positions[index];
                var length = positions[index + 1] - start - delimiter.Length;
                csv[i++] = str.Substring(start, length).Trim();
            }

            return csv;
        }

        public static IEnumerable<int> IndexOfNth(string str, string value, int nth,
            StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            if (nth <= 0)
                throw new ArgumentException("Can not find the zeroth index of substring in string. Must start with 1");

            yield return 0;

            int offset = str.IndexOf(value, comparison);
            int i;
            for (i = 1; i < nth; i++)
            {
                if (offset == -1)
                    break;

                yield return offset + value.Length;

                offset = str.IndexOf(value, offset + 1, comparison);
            }

            if (i != nth)
                yield return str.Length + value.Length;
        }
    }
}
