﻿using AutoFixture;
using FluentAssertions;
using MoreLinq;
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

            var writeOptions = new ParallelismOptions()
            {
                Enabled = parallel,
                EnsureOriginalOrdering = ordered,
            };

            textWriter.WriteRecords(expectedItems, writer.TryFormat, writeOptions);
            textWriter.Flush();

            // Assert

            memory.Seek(0, SeekOrigin.Begin);
            using var textReader = new StreamReader(memory);
            var readOptions = new VariableLengthReaderOptions()
            {
                ParallelismOptions = new() { Enabled = parallel }
            };

            var reads = textReader.ReadRecords(reader, readOptions);

            reads.Should().BeEquivalentTo(expectedItems);
        }

        // the scenario is covered in the above test, because test dont matters what
        // parser is used, since it just receives a delegate
        //[Theory]
        //[MemberData(nameof(Repeats))]
        public void Write_fixed_length_file(int repeat, bool parallel, bool ordered)
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

            var writeOptions = new ParallelismOptions()
            {
                Enabled = parallel,
                EnsureOriginalOrdering = ordered
            };

            textWriter.WriteRecords(expectedItems, writer.TryFormat, writeOptions);
            textWriter.Flush();

            // Assert

            memory.Seek(0, SeekOrigin.Begin);
            using var textReader = new StreamReader(memory);
            var readOptions = new FixedLengthReaderOptions<(string Name, DateTime Birthday, decimal Money, Color Color)>()
            {
                Parser = reader.Parse,
                ParallelismOptions = new() { Enabled = parallel }
            };

            var reads = textReader.ReadRecords(readOptions);

            reads.Should().BeEquivalentTo(expectedItems);
        }
    }
}