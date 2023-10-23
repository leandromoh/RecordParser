using AutoFixture;
using FluentAssertions;
using MoreLinq;
using RecordParser.Builders.Reader;
using RecordParser.Builders.Writer;
using RecordParser.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class FileWriterTest : TestSetup
    {
        private static readonly IEnumerable<int> _repeats = new[] { 0, 1, 3, 1_000, 10_000 };
        private const int MaxParallelism = 4;

        public static IEnumerable<object[]> Repeats()
        {
            foreach (var repeat in _repeats)
            {
                foreach (var parallel in new[] { true, false })
                {
                    foreach (var ordered in new[] { true, false })
                    {
                        yield return new object[] { repeat, parallel, ordered };
                    }
                }
            }
        }

        // the fixed-length file scenario is already covered in the test bellow,
        // because "WriteRecords" method dont matters what parser is used,
        // since it just receives a delegate
        [Theory]
        [MemberData(nameof(Repeats))]
        public void Write_csv_file(int repeat, bool parallel, bool ordered)
        {
            // Arrange

            const string separator = ";";

            var expectedItems = new Fixture()
                .CreateMany<(string Name, DateTime Birthday, decimal Money, Color Color, int Index)>(1_000)
                .Repeat()
                .Take(repeat)
                .Select((x, i) => { x.Index = i; return x; })
                .ToList();

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color, int Index)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, (dest, value) => (value.Ticks.TryFormat(dest, out var written), written))
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Map(x => x.Index, 4)
                .Build(separator);

            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color, int Index)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, value => new DateTime(long.Parse(value)))
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Map(x => x.Index, 4)
                .Build(separator);

            // Act

            using var memory = new MemoryStream();
            using var textWriter = new StreamWriter(memory);

            var parallelOptions = new ParallelismOptions()
            {
                Enabled = parallel,
                EnsureOriginalOrdering = ordered,
                MaxDegreeOfParallelism = MaxParallelism,
            };

            textWriter.WriteRecords(expectedItems, writer.TryFormat, parallelOptions);
            textWriter.Flush();

            // Assert

            memory.Seek(0, SeekOrigin.Begin);
            using var textReader = new StreamReader(memory);
            var readOptions = new VariableLengthReaderOptions()
            {
                ParallelismOptions = parallelOptions
            };

            var items = textReader.ReadRecords(reader, readOptions);

            if (ordered)
                items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
            else
                items.Should().BeEquivalentTo(expectedItems);
        }       
    }
}