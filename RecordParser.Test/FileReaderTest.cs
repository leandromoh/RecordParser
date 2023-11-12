using FluentAssertions;
using MoreLinq;
using RecordParser.Builders.Reader;
using RecordParser.Extensions;
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

        [Theory]
        // note: since new line is \n on unix and \r\n on windows
        // string length will not be the same. so depending of the 
        // values passed, result may differ depending of the OS.
        // current values passes in both.
        [InlineData(900_000, true)]
        [InlineData(1_100_000, false)]
        public void Given_record_is_too_large_for_default_buffer_size_then_exception_should_be_throw(int innerRecords, bool enoughBuffer)
        {
            // Arrange

            // construct a CSV with a header row
            // and a *single* data row, where the 4th column contains a large inlined, CSV file enclosed in quotes.
            // this is an extreme case, but is a valid CSV according to the spec. 
            var tw = new StringWriter();
            tw.WriteLine("A,B,C,D");
            tw.Write("1,2,3,\"");

            for (int i = 0; i < innerRecords; i++)
            {
                tw.WriteLine("1,2,3,4");
            }
            // close the quoted field
            tw.WriteLine("\"");

            var fileContent = tw.ToString();
            var reader = new StringReader(fileContent);

            // Act

            var options = new VariableLengthReaderRawOptions
            {
                HasHeader = true,
                ContainsQuotedFields = true,
                ColumnCount = 4,
                Separator = ",",
                ParallelismOptions = new()
                {
                    Enabled = true,
                    MaxDegreeOfParallelism = 2
                },
            };

            var act = () =>
            {
                var records = reader.ReadRecordsRaw(options, getField =>
                {
                    var record = new
                    {
                        A = getField(0),
                        B = getField(1),
                        C = getField(2),
                        D = getField(3)
                    };
                    return record;
                });

                return records.ToList();
            };

            // Assert

            if (enoughBuffer == false)
            {
                act.Should().Throw<RecordTooLargeException>().WithMessage("Record is too large.");
                return;
            }

            var result = act();
            result.Should().HaveCount(1);
            var row = result[0];
            row.A.Should().Be("1");
            row.B.Should().Be("2");
            row.C.Should().Be("3");

            var start = fileContent.IndexOf('"') + 1;
            var end = fileContent.LastIndexOf('"');
            var innerCSV = fileContent.AsSpan(start, end - start);

            row.D.Should().Be(innerCSV);
        }

        [Theory(Skip = "At the moment lib does not support customized buffer size")]
        [InlineData(12, 4)]
        [InlineData(13, 5)]
        [InlineData(14, 7)]
        [InlineData(15, 7)]
        public void Given_record_is_too_large_for_custom_buffer_size_then_exception_should_be_throw(int bufferSize, int canRead)
        {
            // Arrange

            var fileContent = """
                A,B,C,D
                1,2,3,4
                5,6,7,8
                9,10,11,12
                13,14,15,16
                87,88,89,100
                89,99,100,101
                88,89,90,91
                """;

            var expected = new[]
            {
                (1,2,3,4),
                (5,6,7,8),
                (9,10,11,12),
                (13,14,15,16),
                (87,88,89,100),
                (89,99,100,101),
                (88,89,90,91),
            };

            var reader = new StringReader(fileContent);

            var parser = new VariableLengthReaderSequentialBuilder<(int A, int B, int C, int D)>()
                .Map(x => x.A)
                .Map(x => x.B)
                .Map(x => x.C)
                .Map(x => x.D)
                .Build(",");

            // Act

            var results = new List<(int A, int B, int C, int D)>();
            var act = () =>
            {
                var records = reader.ReadRecords(parser, new()
                {
                    HasHeader = true,
                //  BufferSize = bufferSize,
                });

                foreach (var item in records)
                    results.Add(item);
            };

            // Assert

            var bufferLargeEnoughToReadAll = canRead == expected.Length;

            if (bufferLargeEnoughToReadAll)
                act();
            else
                act.Should().Throw<RecordTooLargeException>().WithMessage("Record is too large.");

            results.Should().BeEquivalentTo(expected.Take(canRead));
        }

        public static string GetFilePath(string fileName) => Path.Combine(Directory.GetCurrentDirectory(), fileName);

        public static IEnumerable<object[]> Given_quoted_csv_file_should_read_quoted_properly_theory(string file)
        {
            var fileNames = new[]
            {
                GetFilePath(file),
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
        [MemberData(nameof(Given_quoted_csv_file_should_read_quoted_properly_theory), new object[] { "AllFieldsQuotedCsv.csv" })]
        public void Read_csv_file_all_fields_quoted(string fileContent, bool hasHeader, bool parallelProcessing, bool blankLineAtEnd, int repeat)
        {
            // Arrange

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderBuilder<PersonComplete>()
                .Map(x => x.id, 0)
                .Map(x => x.name, 1)
                .Map(x => x.age, 2)
                .Map(x => x.birthday, 3)
                .Map(x => x.gender, 4)
                .Map(x => x.email, 5)
                .Map(x => x.children, 7)
                .Build(",");

            var expectedItems = new PersonComplete[]
            {
                new () { id = new Guid("ec9a8be9-a000-503b-adcf-7266804f1eb1"), name = "Lilly Bradley", age = 21, birthday = DateTime.Parse("11/16/1977"), gender = Gender.Male, email = "pak@witak.bf", children = true },
                new () { id = new Guid("63858071-cbb3-5abd-9f88-3dfd565cc4ab"), name = "Lucy Berry", age = 49, birthday = DateTime.Parse("11/12/1961"), gender = Gender.Female, email = "vanvo@ro.pk", children = false },
                new () { id = new Guid("203804f9-93e7-5510-8bb2-177296bafe6a"), name = "Frank Fox", age = 36, birthday = DateTime.Parse("3/19/1977"), gender = Gender.Male, email = "vav@ped.fj", children = true },
                new () { id = new Guid("a8af66fb-bad4-51eb-810c-bf3ca22337c6"), name = "Isabel Todd", age = 51, birthday = DateTime.Parse("9/16/1999"), gender = Gender.Female, email = "gu@or.bz", children = false },
                new () { id = new Guid("1a3d8a66-3e0c-50eb-99c1-a3926bce15ed"), name = $"Joseph {Environment.NewLine}Scott", age = 55, birthday = DateTime.Parse("10/26/1986"), gender = Gender.Male, email = "bup@vugeb.tt", children = false },
                new () { id = new Guid("aa7d4395-f10f-5776-9912-e3d86c4b9d3c"), name = "Gilbert Brooks", age = 56, birthday = DateTime.Parse("3/1/1956"), gender = Gender.Female, email = "epiju@ba.ly", children = true },
                new () { id = new Guid("1d25b811-4002-5744-ac40-93a50f2a442c"), name = "Louis \"Ronaldo\" Bennett", age = 25, birthday = DateTime.Parse("4/4/1967"), gender = Gender.Male, email = "ma@itrovive.tv", children = true },
                new () { id = new Guid("8e963ae5-a9ed-5572-b11c-566abc6a8a56"), name = "Norman Parker", age = 57, birthday = DateTime.Parse("4/17/1969"), gender = Gender.Male, email = "omi@hewepa.bw", children = true },
                new () { id = new Guid("4d373cfb-79e3-54ce-87ff-f2a08fde8f28"), name = "Gary Doyle", age = 20, birthday = DateTime.Parse("1/21/1958"), gender = Gender.Male, email = "orjohma@cabmofa.ps", children = true },
                new () { id = new Guid("5af00cdf-0758-5317-bcdf-c9a3337cc266"), name = "Bruce Silva", age = 39, birthday = DateTime.Parse("1/11/1968"), gender = Gender.Female, email = "ta@ovonib.ir", children = true },
            }
            .Repeat(repeat);

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = hasHeader,
                ParallelismOptions = new() { Enabled = parallelProcessing },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(Given_quoted_csv_file_should_read_quoted_properly_theory), new object[] { "QuotedCsv.csv" })]
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
                ParallelismOptions = new() { Enabled = parallelProcessing },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

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
                ParallelismOptions = new() { Enabled = parallelProcessing },
                ContainsQuotedFields = containgQuote,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

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
                ParallelismOptions = new() { Enabled = parallelProcessing },
                Parser = Parse
            };

            // Act

            var records = streamReader.ReadRecords(readOptions);

            var linesByType = records.ToLookup(x => x.GetType());
            var result = linesByType[typeof(HeaderFixedLength)].Cast<HeaderFixedLength>().Single();
            var items = linesByType[typeof(RecordFixedLength)].Cast<RecordFixedLength>();
            result.Items = items;

            // Assert

            result.Count.Should().Be(repeat);
            result.Items.Count().Should().Be(repeat);
            result.Should().BeEquivalentTo(expected);
            result.Items.Should().BeEquivalentTo(expected.Items, cfg => cfg.WithStrictOrdering());

            object Parse(ReadOnlySpan<char> line)
            {
                var lineType = line[0];

                switch (lineType)
                {
                    case '0':
                        return headerReader.Parse(line);

                    case '9':
                        return recordReader.Parse(line);

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
            var line = string.Empty;

            while ((line = streamReader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    expected.Add(line);
            }

            fileStream.Position = 0;

            // Act

            var result = new List<string>();

            foreach (var item in streamReader.ReadRecords())
            {
                result.Add(item.Span.ToString());
            }

            // Assert

            result.Should().BeEquivalentTo(expected, cfg => cfg.WithStrictOrdering());
        }
    }
}