using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Extensions.FileReader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RecordParser.Test
{
    public class FileReaderTest
    {
        public class Quoted
        {
            public int Id;
            public DateTime Date;
            public string Name;
            public string Rate;
            public int Ranking;
        }

        public static string GetFilePath(string fileName) => Path.Combine(Directory.GetCurrentDirectory(), fileName);

        public static IEnumerable<object[]> Given_text_mapped_should_write_quoted_properly_theory()
        {
            var fileNames = new[]
            {
                GetFilePath("QuotedCsv.csv"),
            };

            foreach (var fileName in fileNames)
            {
                foreach (var parallel in new[] { true, false })
                {
                    foreach (var hasHeader in new[] { true, false })
                    {
                        foreach (var blankLineAtEnd in new[] { true, false })
                        {
                            var fileBuilder = new StringBuilder(File.ReadAllText(fileName));

                            if (hasHeader)
                                fileBuilder.Insert(index: 0, "Id,Date,Name,Rate,Ranking" + Environment.NewLine);

                            if (blankLineAtEnd)
                                fileBuilder.AppendLine();

                            var fileText = fileBuilder.ToString();

                            yield return new object[] { fileText, hasHeader, parallel, blankLineAtEnd };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Given_text_mapped_should_write_quoted_properly_theory))]
        public async Task Read_file_using_(string fileContent, bool hasHeader, bool parallelProcessing, bool blankLineAtEnd)
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
            };

            var readOptions = new VariableLengthReaderOptions
            {
                hasHeader = hasHeader,
                parallelProcessing = parallelProcessing,
                containsQuotedFields = true,
            };

            // Act

            var items = parser.GetRecords(streamReader, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        public static IEnumerable<object[]> Given_text_mapped_should_write_quoted_properly_theory_simple_csv()
        {
            var fileNames = new[]
            {
                GetFilePath("SimpleCsv.csv"),
            };

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
                                var fileBuilder = new StringBuilder(File.ReadAllText(fileName));

                                if (hasHeader)
                                    fileBuilder.Insert(index: 0, "Id,Date,Name,Rate,Ranking" + Environment.NewLine);

                                if (blankLineAtEnd)
                                    fileBuilder.AppendLine();

                                var fileText = fileBuilder.ToString();

                                yield return new object[] { fileText, hasHeader, parallel, blankLineAtEnd, quote };
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Given_text_mapped_should_write_quoted_properly_theory_simple_csv))]
        public async Task Read_file_using_simple_csv(string fileContent, bool hasHeader, bool parallelProcessing, bool blankLineAtEnd, bool containgQuote)
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
            };

            var readOptions = new VariableLengthReaderOptions
            {
                hasHeader = hasHeader,
                parallelProcessing = parallelProcessing,
                containsQuotedFields = containgQuote,
            };

            // Act

            var items = parser.GetRecords(streamReader, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }
    }
}
