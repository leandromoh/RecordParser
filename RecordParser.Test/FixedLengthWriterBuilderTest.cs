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
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
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

            Span<char> destination = stackalloc char[50];
            var charsWritten = writer.Parse(instance, destination);
            var result = destination.Slice(0, charsWritten).ToString();

            var expected = string.Join('\0', new[]
            {
                instance.Name.PadRight(15, ' '),
                instance.Birthday.ToString("yyyy.MM.dd"),
                ((int)(instance.Money * 100)).ToString().PadRight(7, ' '),
                instance.Color.ToString().PadLeft(15, '-'),
            });

            result.Should().Be(expected);
        }


        [Fact]
        public void Given_tooLargeObjetc()
        {
            var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
               .Map(x => x.Name, 0, 15, paddingChar: ' ')
               .Map(x => x.Birthday, 16, 10, "yyyy.MM.dd")
               //  .Map(x => x.Money, 2)
               .Map(x => x.Color, 27, 15, padding: Padding.Left, paddingChar: '-')
               .BuildForUnitTest();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 0123.45M,
                            Color: Color.LightBlue);

            Span<char> destination = stackalloc char[16];
            var charsWritten = writer.Parse(instance, destination);

            var result = destination.Slice(0, charsWritten).ToString();

            charsWritten.Should().Be(0);
            result.Should().Be("");
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
            return builder.Map(ex, startIndex, length,
                (span, value) => (((int)(value * (decimal)Math.Pow(10, precision))).TryFormat(span, out var written, format), written),
                padding, paddingChar);
        }
    }
}
