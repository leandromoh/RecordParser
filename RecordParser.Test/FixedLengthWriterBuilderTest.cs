using FluentAssertions;
using RecordParser.BuilderWrite;
using RecordParser.Generic;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthWriterBuilderTest
    {
        [Theory]
        [InlineData(50)]
        [InlineData(51)]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration(int destinationSize)
        {
            // Arrange

            var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, 15, paddingChar: ' ')
                .Map(x => x.Birthday, 16, 10, "yyyy.MM.dd")
                .Map(x => x.Money, 27, 7, precision: 2)
                .Map(x => x.Color, 35, 15, padding: Padding.Left, paddingChar: '-')
                .BuildForUnitTest();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 0123.456M,
                            Color: Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var charsWritten = writer.Parse(instance, destination);

            // Assert

            charsWritten.Should().Be(50);

            var expected = string.Join('\0', new[]
            {
                instance.Name.PadRight(15, ' '),
                instance.Birthday.ToString("yyyy.MM.dd"),
                ((int)(instance.Money * 100)).ToString().PadRight(7, ' '),
                instance.Color.ToString().PadLeft(15, '-'),
            });

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData(49)]
        [InlineData(48)]
        [InlineData(01)]
        [InlineData(00)]
        public void Given_destination_shorter_than_max_position_especified_should_write_nothing(int destinationSize)
        {
            // Arrange

            var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, 15, paddingChar: ' ')
                .Map(x => x.Birthday, 16, 10, "yyyy.MM.dd")
                .Map(x => x.Money, 27, 7, precision: 2, padding: Padding.Left, paddingChar: '0')
                .Map(x => x.Color, 35, 15, padding: Padding.Left, paddingChar: '-')
                .BuildForUnitTest();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 0123.456M,
                            Color: Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var charsWritten = writer.Parse(instance, destination);

            // Assert

            charsWritten.Should().Be(0);
            destination.ToString().Should().Be(new string(default, destinationSize));
        }

        [Fact]
        public void Given_string_value_larger_than_designated_space_should_write_until_reach_it()
        {
            // Arrange

            var writer = new FixedLengthWriterBuilder<(DateTime Birthday, string Name)>()
                .Map(x => x.Birthday, 0, 10, "yyyy.MM.dd")
                .Map(x => x.Name, 11, 15, paddingChar: ' ')
                .BuildForUnitTest();

            var instance = (Birthday: new DateTime(2020, 05, 23),
                            Name: "foo bar baz 3456");

            Span<char> destination = stackalloc char[50];

            // Act

            var charsWritten = writer.Parse(instance, destination);

            // Assert

            charsWritten.Should().Be(10);

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("2020.05.23");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_non_string_value_larger_than_designated_space_should_write_until_reach_it()
        {
            // Arrange

            var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday)>()
                .Map(x => x.Name, 0, 15, paddingChar: ' ')
                .Map(x => x.Birthday, 16, 9, "yyyy.MM.dd")
                .BuildForUnitTest();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23));

            Span<char> destination = stackalloc char[50];

            // Act

            var charsWritten = writer.Parse(instance, destination);

            // Assert

            charsWritten.Should().Be(15);

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo bar baz    ");
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }

    public static class FixedLengthWriterHelpers
    {
        public static IFixedLengthWriter<T> BuildForUnitTest<T>(this IFixedLengthWriterBuilder<T> source)
            => source.Build(CultureInfo.InvariantCulture);

        public static IVariableLengthWriter<T> BuildForUnitTest<T>(this IVariableLengthWriterBuilder<T> source, string separator)
            => source.Build(separator, CultureInfo.InvariantCulture);

        public static IFixedLengthWriterBuilder<T> Map<T>(this IFixedLengthWriterBuilder<T> builder, Expression<Func<T, decimal>> ex, int startIndex, int length, int precision, string format = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var multiply = (int)Math.Pow(10, precision);

            return builder.Map(ex, startIndex, length,
                (span, value) => (((int)(value * multiply)).TryFormat(span, out var written, format), written),
                padding, paddingChar);
        }
    }
}
