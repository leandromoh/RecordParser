using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Extensions;
using System;
using System.Collections.Generic;
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

        public static IEnumerable<object[]> QuotedFieldsAnyColumn()
        {
            foreach (var parallel in new[] { true, false })
            {
                foreach (var newline in new[] { "\r", "\n", "\r\n" })
                {
                    yield return new object[] { parallel, newline };
                }
            }
        }

        [Theory]
        [MemberData(nameof(QuotedFieldsAnyColumn))]
        public void Given_quoted_field_in_any_column_should_parse_successfully(bool parallel, string newline)
        {
            // Arrange

            var fileContent = """
                A,B,C,D
                "x
                y",2,3,4
                1,"a,
                b",3,4
                7,8,"a
                z",9
                98,99,100,101
                12,13,14,"w
                s"
                  "
                a,1
                ", 3,b , "4"
                a,b,c,d
                """.Replace(Environment.NewLine, newline);

            var expected = new[]
            {
                // column quoted = 1
                ($"x{newline}y","2","3","4"),
                // column quoted = 2
                ("1",$"a,{newline}b","3","4"),
                // column quoted = 3
                ("7","8",$"a{newline}z","9"),
                // no quoted column
                ("98","99","100","101"),
                // column quoted = 4
                ("12","13","14",$"w{newline}s"),
                // column quoted = 1 with leading & trailing whitespace
                ($"{newline}a,1{newline}"," 3","b ","4"),
                // no quoted column
                ("a","b","c","d"),
            };

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
                    MaxDegreeOfParallelism = 2,
                    EnsureOriginalOrdering = true,
                },
            };

            // Act

            var records = reader.ReadRecordsRaw(options, getField =>
            {
                var record =
                (
                    getField(0),
                    getField(1),
                    getField(2),
                    getField(3)
                );
                return record;
            }).ToList();

            // Assert

            records.Should().BeEquivalentTo(expected, cfg => cfg.WithStrictOrdering());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Given_quoted_field_in_first_column_should_parse_successfully(bool parallel)
        {
            // Arrange

            var fileContent = $"""
                A,B,C,D
                "x
                y",2,3,4
                """;

            var expected = ($"x{Environment.NewLine}y","2","3","4");
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
                    MaxDegreeOfParallelism = 2,
                },
            };

            // Act

            var result = reader.ReadRecordsRaw(options, getField =>
            {
                var record =
                (
                    getField(0),
                    getField(1),
                    getField(2),
                    getField(3)
                );
                return record;
            }).ToList();

            // Assert

            result.Should().HaveCount(1);
            var row = result[0];
            row.Should().Be(expected);
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