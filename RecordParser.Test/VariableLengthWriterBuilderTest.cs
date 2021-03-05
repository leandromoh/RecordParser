using FluentAssertions;
using RecordParser.BuilderWrite;
using System;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthWriterBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)> ()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be("foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_skip_column_using_standard_format_should_parse_without_extra_configuration()
        {
            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Color, 4)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be("foo bar baz ; 2020.05.23 ;  ;  ; LightBlue");
        }

        [Fact]
        public void Given_skip_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 3)
                .Map(x => x.Color, 4)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_skip2_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(0, charsWritten).ToString();

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue");
        }

        [Fact]
        public void Given_value_should_not_write_in_span_more_than_return_charswritten()
        {
            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, indexColumn: 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M);

            Span<char> destination = stackalloc char[100];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(charsWritten).ToString();

            var expected = new string(default, result.Length);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(99, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(52, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(51, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]

        [InlineData(50, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(43, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(42, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]

        [InlineData(41, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(40, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(39, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]

        [InlineData(38, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(34, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(33, " ; foo bar baz ; 2020.05.23 ;  ; ")]

        [InlineData(32, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(31, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(30, " ; foo bar baz ; 2020.05.23 ; ")]

        [InlineData(29, " ; foo bar baz ; 2020.05.23")]
        [InlineData(28, " ; foo bar baz ; 2020.05.23")]
        [InlineData(27, " ; foo bar baz ; 2020.05.23")]

        [InlineData(26, " ; foo bar baz ; ")]
        [InlineData(18, " ; foo bar baz ; ")]
        [InlineData(17, " ; foo bar baz ; ")]

        [InlineData(16, " ; foo bar baz")]
        [InlineData(15, " ; foo bar baz")]
        [InlineData(14, " ; foo bar baz")]

        [InlineData(13, " ; ")]
        [InlineData(04, " ; ")]
        [InlineData(03, " ; ")]

        [InlineData(02, "")]
        [InlineData(01, "")]
        [InlineData(00, "")]
        public void Given_too_short_destination_should_write_while_have_enough_space_for_current_field(int destinationSize, string expected)
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .BuildForUnitTest(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act
            
            var charsWritten = writer.Parse(instance, destination);

            // Assert

            charsWritten.Should().Be(expected.Length);

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }
}
