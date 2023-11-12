using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RecordParser.Test
{
    public class QuotedFileReaderTest : TestSetup
    {
        public class Quoted
        {
            public int Id;
            public DateTime Date;
            public string Name;
            public Gender Gender;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void foo(bool parallel)
        {
            // Arrange

            var fileContent = """
                A,B,C,D
                "x
                y",2,3,4
                """;

            var reader = new StringReader(fileContent);
            var options = new VariableLengthReaderRawOptions
            {
                HasHeader = true,
                ContainsQuotedFields = true,
                ColumnCount = 4,
                Separator = ",",
                ParallelismOptions = new()
                {
                    Enabled = parallel,
                    MaxDegreeOfParallelism = 2
                },
            };

            // Act

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
            }).ToList();

            // Assert

            records.Should().HaveCount(1);

            var record = records.Single();
            record.A.Should().Be("x\r\ny");
            record.B.Should().Be("2");
            record.C.Should().Be("3");
            record.D.Should().Be("4");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_csv_file_all_fields_unquoted(bool parallel)
        {
            // Arrange

            var fileContent = """
                1,2023-10-29,Foo Bar,Male
                2,2022-11-28,ABC XYZ,Female
                """;

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderSequentialBuilder<Quoted>()
                .Map(x => x.Id)
                .Map(x => x.Date)
                .Map(x => x.Name)
                .Map(x => x.Gender)
                .Build(",");

            var expectedItems = new Quoted[]
            {
                new () { Id = 1, Date = DateTime.Parse("10/29/2023"), Name = "Foo Bar", Gender = Gender.Male },
                new () { Id = 2, Date = DateTime.Parse("11/28/2022"), Name = "ABC XYZ", Gender = Gender.Female }
            };

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_csv_file_all_fields_quoted(bool parallel)
        {
            // Arrange

            var fileContent = """
                "1","2023-10-29","Foo Bar","Male"
                "2","2022-11-28","ABC XYZ","Female"
                """;

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderSequentialBuilder<Quoted>()
                .Map(x => x.Id)
                .Map(x => x.Date)
                .Map(x => x.Name)
                .Map(x => x.Gender)
                .Build(",");

            var expectedItems = new Quoted[]
            {
                new () { Id = 1, Date = DateTime.Parse("10/29/2023"), Name = "Foo Bar", Gender = Gender.Male },
                new () { Id = 2, Date = DateTime.Parse("11/28/2022"), Name = "ABC XYZ", Gender = Gender.Female }
            };

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_csv_file_with_quoted_comma(bool parallel)
        {
            // Arrange

            var fileContent = """
                "1","2023-10-29","Foo,Bar","Male"
                "2","2022-11-28","ABC XYZ","Female"
                """;

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderSequentialBuilder<Quoted>()
                .Map(x => x.Id)
                .Map(x => x.Date)
                .Map(x => x.Name)
                .Map(x => x.Gender)
                .Build(",");

            var expectedItems = new Quoted[]
            {
                new () { Id = 1, Date = DateTime.Parse("10/29/2023"), Name = "Foo,Bar", Gender = Gender.Male },
                new () { Id = 2, Date = DateTime.Parse("11/28/2022"), Name = "ABC XYZ", Gender = Gender.Female }
            };

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_csv_file_with_quoted_new_line(bool parallel)
        {
            // Arrange

            var fileContent = $"""
                "1","2023-10-29","Foo{"\n"}Bar","Male"
                "2","2022-11-28","ABC{"\r\n"}XYZ","Female"
                """;

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderSequentialBuilder<Quoted>()
                .Map(x => x.Id)
                .Map(x => x.Date)
                .Map(x => x.Name)
                .Map(x => x.Gender)
                .Build(",");

            var expectedItems = new Quoted[]
            {
                new () { Id = 1, Date = DateTime.Parse("10/29/2023"), Name = "Foo\nBar", Gender = Gender.Male },
                new () { Id = 2, Date = DateTime.Parse("11/28/2022"), Name = "ABC\r\nXYZ", Gender = Gender.Female }
            };

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Read_csv_file_with_quoted_quote(bool parallel)
        {
            // Arrange

            var fileContent = """
                "1","2023-10-29","Foo""Bar","Male"
                "2","2022-11-28","ABC XYZ","Female"
                """;

            using var fileStream = fileContent.ToStream();
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8);

            var parser = new VariableLengthReaderSequentialBuilder<Quoted>()
                .Map(x => x.Id)
                .Map(x => x.Date)
                .Map(x => x.Name)
                .Map(x => x.Gender)
                .Build(",");

            var expectedItems = new Quoted[]
            {
                new () { Id = 1, Date = DateTime.Parse("10/29/2023"), Name = "Foo\"Bar", Gender = Gender.Male },
                new () { Id = 2, Date = DateTime.Parse("11/28/2022"), Name = "ABC XYZ", Gender = Gender.Female }
            };

            var readOptions = new VariableLengthReaderOptions
            {
                HasHeader = false,
                ParallelismOptions = new() { Enabled = parallel },
                ContainsQuotedFields = true,
            };

            // Act

            var items = streamReader.ReadRecords(parser, readOptions);

            // Assert

            items.Should().BeEquivalentTo(expectedItems, cfg => cfg.WithStrictOrdering());
        }
    }
}