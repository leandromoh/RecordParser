using AutoFixture;
using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Builders.Writer;
using RecordParser.Extensions.FileReader;
using RecordParser.Extensions.FileWriter;
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

        public static IEnumerable<object[]> Repeats() => _repeats.Select(x => new object[] { x });

        [Theory]
        [MemberData(nameof(Repeats))]
        public void Write_csv_file(int repeat)
        {
            // Arrange

            const string separator = ";";

            var expectedItems = new Fixture()
               .CreateMany<(string Name, DateTime Birthday, decimal Money, Color Color)>(repeat)
               .ToList();

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, (dest, value) => (value.Ticks.TryFormat(dest, out var written), written))
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(separator);

            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, value => new DateTime(long.Parse(value)))
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(separator);

            // Act

            using var memory = new MemoryStream();
            using var textWriter = new StreamWriter(memory);

            expectedItems.Write(textWriter, writer.TryFormat);
            textWriter.Flush();

            // Assert

            memory.Seek(0, SeekOrigin.Begin);
            using var textReader = new StreamReader(memory);
            var readOptions = new VariableLengthReaderOptions();
            var reads = reader.GetRecords(textReader, readOptions);

            reads.Should().BeEquivalentTo(expectedItems);
        }

        [Theory]
        [MemberData(nameof(Repeats))]
        public void Write_fixed_length_file(int repeat)
        {
            // Arrange

            var expectedItems = new Fixture()
               .CreateMany<(string Name, DateTime Birthday, decimal Money, Color Color)>(repeat)
               .ToList();

            var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 50)
                .Map(x => x.Birthday, 20, (dest, value) => (value.Ticks.TryFormat(dest, out var written), written))
                .Map(x => x.Money, 15)
                .Map(x => x.Color, 15)
                .Build();

            var reader = new FixedLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 50)
                .Map(x => x.Birthday, 20, value => new DateTime(long.Parse(value)))
                .Map(x => x.Money, 15)
                .Map(x => x.Color, 15)
                .Build();

            // Act

            using var memory = new MemoryStream();
            using var textWriter = new StreamWriter(memory);

            expectedItems.Write(textWriter, writer.TryFormat);
            textWriter.Flush();

            // Assert

            memory.Seek(0, SeekOrigin.Begin);
            using var textReader = new StreamReader(memory);
            var readOptions = new FixedLengthReaderOptions<(string Name, DateTime Birthday, decimal Money, Color Color)>()
            {
                Parser = reader.Parse
            };

            var reads = textReader.GetRecords(readOptions);

            reads.Should().BeEquivalentTo(expectedItems);
        }
    }
}