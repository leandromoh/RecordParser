using RecordParser.Parsers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RecordParser.Extensions
{
	public static partial class Exasd
	{
		// 45
		public static IEnumerable<T> GetRecordsParallel<T>(this IVariableLengthReader<T> reader, TextReader stream, bool hasHeader)
		{
            var items = new RowByLine(stream, length);

            if (items.FillBufferAsync() > 0 == false)
            {
                yield break;
            }

            foreach (var x in items.TryReadLine().Skip(hasHeader ? 1 : 0).AsParallel().Select(x => reader.Parse(x.Span)))
            {
                yield return x;
            }

            while (items.FillBufferAsync() > 0)
            {
                foreach (var x in items.TryReadLine().AsParallel().Select(x => reader.Parse(x.Span)))
                {
                    yield return x;
                }
            }
        }
	}
}
