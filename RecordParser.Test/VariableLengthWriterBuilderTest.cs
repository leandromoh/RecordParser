using FluentAssertions;
using RecordParser.BuilderWrite;
using System;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthWriterBuilderTest : TestSetup
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)> ()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be("foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_skip_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Color, 4)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be("foo bar baz ; 2020.05.23 ;  ;  ; LightBlue");
        }

        [Fact]
        public void Given_skip_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 3)
                .Map(x => x.Color, 4)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_skip2_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_value_should_not_write_in_span_more_than_return_charswritten()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, indexColumn: 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            var result = destination.Slice(charsWritten).ToString();

            var expected = new string(default, result.Length);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(99, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(52, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(51, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]

        [InlineData(50, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(43, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(42, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
                       
        [InlineData(41, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(40, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(39, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
                       
        [InlineData(38, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(34, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(33, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]
                       
        [InlineData(32, false, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(31, false, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(30, false, " ; foo bar baz ; 2020.05.23 ; ")]
                       
        [InlineData(29, false, " ; foo bar baz ; 2020.05.23")]
        [InlineData(28, false, " ; foo bar baz ; 2020.05.23")]
        [InlineData(27, false, " ; foo bar baz ; 2020.05.23")]
                       
        [InlineData(26, false, " ; foo bar baz ; ")]
        [InlineData(18, false, " ; foo bar baz ; ")]
        [InlineData(17, false, " ; foo bar baz ; ")]
                       
        [InlineData(16, false, " ; foo bar baz")]
        [InlineData(15, false, " ; foo bar baz")]
        [InlineData(14, false, " ; foo bar baz")]
                       
        [InlineData(13, false, " ; ")]
        [InlineData(04, false, " ; ")]
        [InlineData(03, false, " ; ")]
                       
        [InlineData(02, false, "")]
        [InlineData(01, false, "")]
        [InlineData(00, false, "")]
        public void Given_too_short_destination_should_write_while_have_enough_space(int destinationSize, bool successfulExpected, string expected)
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act
            
            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().Be(successfulExpected);
            charsWritten.Should().Be(expected.Length);

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            // Arrange

            var writer = new VariableLengthWriterBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Debit, 0)
                .Map(x => x.Balance, 1)
                .Map(x => x.Date, 2)
                .DefaultTypeConvert<decimal>((span, value) => (((long)(value * 100)).TryFormat(span, out var written), written))
                .DefaultTypeConvert<DateTime>((span, value) => (value.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Build(" ; ");

            var instance = (Debit: 0123.45M,
                            Balance: 0123456789.01M,
                            Date: new DateTime(2020, 05, 23));

            Span<char> destination = stackalloc char[31];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            charsWritten.Should().Be(30);

            var expected = "12345 ; 12345678901 ; 23052020";

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            // Arrange

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 1, (span, text) => (true, text.AsSpan().ToUpperInvariant(span)))
                .Map(x => x.Birthday, 2, (span, date) => (date.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Map(x => x.Money, 3)
                .Map(x => x.Nickname, 4, (span, text) => (true, text.AsSpan().Slice(0, 4).ToUpperInvariant(span)))
                .Build(" ; ");

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 123.45M,
                            Nickname: "nick name");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            charsWritten.Should().Be(41);

            var expected = " ; FOO BAR BAZ ; 23052020 ; 123.45 ; NICK";

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            // Assert

            var writer = new VariableLengthWriterBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(x => x.MotherAge, 1)
                .Map(x => x.FatherAge, 2)
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build(" ; ");

            var instance = (Age: 15,
                            MotherAge: 40,
                            FatherAge: 50);

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            charsWritten.Should().Be(12);

            var expected = "30 ; 42 ; 52";

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }
}
