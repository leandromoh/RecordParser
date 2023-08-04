using FluentAssertions;
using MoreLinq;
using RecordParser.Builders.Reader;
using RecordParser.Extensions.FileReader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RecordParser.Test
{
    public class FileReaderTest : TestSetup
    {
        private static readonly IEnumerable<int> _repeats = new[] { 0, 1, 3, 1_000, 10_000 };

        public class Quoted
        {
            public int Id;
            public DateTime Date;
            public string Name;
            public string Rate;
            public int Ranking;
        }

        public static string GetFilePath(string fileName) => Path.Combine(Directory.GetCurrentDirectory(), fileName);

        public static IEnumerable<object[]> Given_quoted_csv_file_should_read_quoted_properly_theory()
        {
            var fileNames = new[]
            {
                GetFilePath("QuotedCsv.csv"),
            };

            foreach (var repeat in _repeats)
            {
                foreach (var fileName in fileNames)
                {
                    foreach (var parallel in new[] { true, false })
                    {
                        foreach (var hasHeader in new[] { true, false })
                        {
                            foreach (var blankLineAtEnd in new[] { true, false })
                            {
                                var fileBuilder = new StringBuilder();
                                var content = File.ReadAllText(fileName);

                                for (int i = 0; i < repeat; i++)
                                {
                                    fileBuilder.Append(content);

                                    var lastIteration = i == repeat - 1;
                                    if (lastIteration is false)
                                        fileBuilder.AppendLine();
                                }

                                if (hasHeader)
                                    fileBuilder.Insert(index: 0, "Id,Date,Name,Rate,Ranking" + Environment.NewLine);

                                if (blankLineAtEnd)
                                    fileBuilder.AppendLine();

                                var fileText = fileBuilder.ToString();

                                yield return new object[] { fileText, hasHeader, parallel, blankLineAtEnd, repeat };
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Given_quoted_csv_file_should_read_quoted_properly_theory))]
        public void Read_quoted_csv_file(string fileContent, bool hasHeader, bool parallelProcessing, bool blankLineAtEnd, int repeat)
        {
            // Arrange

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderBuilder<Quoted>()
                .Map(x => x.Id, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Name, 2)
                .Map(x => x.Rate, 3)
                .Map(x => x.Ranking, 4)
                .Build(",", CultureInfo.InvariantCulture);

            var expectedItems = new Quoted[]
            {
                new Quoted { Id = 1, Date = new DateTime(2010, 01, 02), Name = "Ana", Rate = "Good", Ranking = 56 },
                new Quoted { Id = 2, Date = new DateTime(2011, 05, 12), Name = "Bob", Rate = $"Much {Environment.NewLine}Good", Ranking = 4 },
                new Quoted { Id = 3, Date = new DateTime(2013, 12, 10), Name = "Carla", Rate = "\"Medium\"", Ranking = 5 },
                new Quoted { Id = 4, Date = new DateTime(2015, 03, 03), Name = "Derik", Rate = "Absolute, Awesome", Ranking = 1 },
            }
            .Repeat(repeat);

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = hasHeader,
                ParallelProcessing = parallelProcessing,
                ContainsQuotedFields = true,
            };

            // Act

            var items = parser.GetRecords(streamReader, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        public static IEnumerable<object[]> Given_not_quoted_csv_file_should_read_quoted_properly_theory()
        {
            var fileNames = new[]
            {
                GetFilePath("SimpleCsv.csv"),
            };

            foreach (var repeat in _repeats)
            {
                foreach (var fileName in fileNames)
                {
                    foreach (var quote in new[] { true, false })
                    {
                        foreach (var parallel in new[] { true, false })
                        {
                            foreach (var hasHeader in new[] { true, false })
                            {
                                foreach (var blankLineAtEnd in new[] { true, false })
                                {
                                    var fileBuilder = new StringBuilder();
                                    var content = File.ReadAllText(fileName);

                                    for (int i = 0; i < repeat; i++)
                                    {
                                        fileBuilder.Append(content);

                                        var lastIteration = i == repeat - 1;
                                        if (lastIteration is false)
                                            fileBuilder.AppendLine();
                                    }

                                    if (hasHeader)
                                        fileBuilder.Insert(index: 0, "Id,Date,Name,Rate,Ranking" + Environment.NewLine);

                                    if (blankLineAtEnd)
                                        fileBuilder.AppendLine();

                                    var fileText = fileBuilder.ToString();

                                    yield return new object[] { fileText, hasHeader, parallel, blankLineAtEnd, quote, repeat };
                                }
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Given_not_quoted_csv_file_should_read_quoted_properly_theory))]
        public void Read_not_quoted_csv_file(string fileContent, bool hasHeader, bool parallelProcessing, bool blankLineAtEnd, bool containgQuote, int repeat)
        {
            // Arrange

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderBuilder<Quoted>()
                .Map(x => x.Id, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Name, 2)
                .Map(x => x.Rate, 3)
                .Map(x => x.Ranking, 4)
                .Build(",", CultureInfo.InvariantCulture);

            var expectedItems = new Quoted[]
            {
                new Quoted { Id = 1, Date = new DateTime(2010, 01, 02), Name = "Ana", Rate = "Good", Ranking = 56 },
                new Quoted { Id = 2, Date = new DateTime(2011, 05, 12), Name = "Bob", Rate = "Much \"medium\" Good", Ranking = 4 },
                new Quoted { Id = 3, Date = new DateTime(2013, 12, 10), Name = "Carla", Rate = "Medium", Ranking = 5 },
                new Quoted { Id = 4, Date = new DateTime(2015, 03, 03), Name = "Derik", Rate = "Absolute Awesome", Ranking = 1 },
            }
            .Repeat(repeat);

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = hasHeader,
                ParallelProcessing = parallelProcessing,
                ContainsQuotedFields = containgQuote,
            };

            // Act

            var items = parser.GetRecords(streamReader, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        public static IEnumerable<object[]> Given_fixed_length_file_should_read_quoted_properly_theory()
        {
            foreach (var repeat in _repeats)
            {
                foreach (var parallel in new[] { true, false })
                {
                    foreach (var blankLineAtEnd in new[] { true, false })
                    {
                        var fileBuilder = new StringBuilder();

                        fileBuilder.AppendLine($"0 51b3f287-ddba-402c-993c-d2df68d44163 {repeat.ToString().PadLeft(5, '0')}");

                        for (int i = 0; i < repeat; i++)
                        {
                            fileBuilder.Append($"9 {i.ToString().PadLeft(5, '0')} foo bar baz 2020.05.23 0123.45");

                            var lastIteration = i == repeat - 1;
                            if (lastIteration is false)
                                fileBuilder.AppendLine();
                        }

                        if (blankLineAtEnd)
                            fileBuilder.AppendLine();

                        var fileText = fileBuilder.ToString();

                        yield return new object[] { fileText, parallel, blankLineAtEnd, repeat };
                    }
                }
            }
        }

        class HeaderFixedLength
        {
            public Guid Id;
            public int Count;
            public IEnumerable<RecordFixedLength> Items;
        }

        struct RecordFixedLength
        {
            public int Id;
            public string Name;
            public DateTime Birthday;
            public decimal Money;
        }

        [Theory]
        [MemberData(nameof(Given_fixed_length_file_should_read_quoted_properly_theory))]
        public void Read_fixed_length_file(string fileContent, bool parallelProcessing, bool blankLineAtEnd, int repeat)
        {
            // Arrange

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var headerReader = new FixedLengthReaderSequentialBuilder<HeaderFixedLength>()
                .Skip(2)
                .Map(x => x.Id, length: 36)
                .Skip(1)
                .Map(x => x.Count, 5)
                .Build();

            var recordReader = new FixedLengthReaderSequentialBuilder<RecordFixedLength>()
                .Skip(2)
                .Map(x => x.Id, length: 5)
                .Skip(1)
                .Map(x => x.Name, length: 11)
                .Skip(1)
                .Map(x => x.Birthday, 10)
                .Skip(1)
                .Map(x => x.Money, 7)
                .Build();

            var expectedItems = new[]
                {
                    new RecordFixedLength { Id = 0, Name = "foo bar baz", Birthday = new DateTime(2020, 05, 23), Money = 123.45M }
                }
                .Repeat(repeat)
                .Select((x, i) =>
                {
                    return x with { Id = i };
                })
                .ToArray();

            var expected = new HeaderFixedLength
            {
                Id = new Guid("51b3f287-ddba-402c-993c-d2df68d44163"),
                Count = repeat,
                Items = expectedItems
            };

            var readOptions = new FixedLengthReaderOptions<object>
            {
                ParallelProcessing = parallelProcessing,
                Parser = Parse
            };

            // Act

            var records = streamReader.GetRecords(readOptions);

            var linesByType = records.ToLookup(x => x.GetType());
            var result = linesByType[typeof(HeaderFixedLength)].Cast<HeaderFixedLength>().Single();
            var items = linesByType[typeof(RecordFixedLength)].Cast<RecordFixedLength>();
            result.Items = items;

            // Assert

            result.Count.Should().Be(repeat);
            result.Items.Count().Should().Be(repeat);
            result.Should().BeEquivalentTo(expected);
            result.Items.Should().BeEquivalentTo(expected.Items, cfg => cfg.WithStrictOrdering());

            object Parse(ReadOnlyMemory<char> line, int index)
            {
                var lineType = line.Span[0];

                switch (lineType)
                {
                    case '0':
                        return headerReader.Parse(line.Span);

                    case '9':
                        return recordReader.Parse(line.Span);

                    default:
                        throw new InvalidOperationException($"lineType '{lineType}' is not mapped");
                }
            }
        }

        [Theory]
        [MemberData(nameof(Given_fixed_length_file_should_read_quoted_properly_theory))]
        public void Read_plain_text_of_fixed_length_file(string fileContent, bool parallelProcessing, bool blankLineAtEnd, int repeat)
        {
            // Arrange

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var expected = new List<string>();

            while (streamReader.EndOfStream is false)
            {
                expected.Add(streamReader.ReadLine());
            }

            fileStream.Position = 0;

            // Act

            var result = new List<string>();

            foreach (var item in streamReader.GetRecords())
            {
                result.Add(item.Span.ToString());
            }

            // Assert

            result.Should().BeEquivalentTo(expected, cfg => cfg.WithStrictOrdering());
        }
    }
}