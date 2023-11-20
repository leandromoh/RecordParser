using FluentAssertions;
using RecordParser.Builders.Writer;
using RecordParser.Test;
using System;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthWriterSequentialBuilderTest : TestSetup
    {
        [Theory]
        [InlineData(50)]
        [InlineData(51)]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration(int destinationSize)
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 15, paddingChar: ' ')
                .Skip(1)
                .Map(x => x.Birthday, 10, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Money, 7, precision: 2)
                .Skip(1)
                .Map(x => x.Color, 15, padding: Padding.Left, paddingChar: '-')
                .Build();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 0123.456M,
                            Color: Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            charsWritten.Should().Be(50);

            var expected = string.Join('\0', new[]
            {
                instance.Name.PadRight(15, ' '),
                instance.Birthday.ToString("yyyy.MM.dd"),
                ((int)(instance.Money * 100)).ToString().PadRight(7, ' '),
                instance.Color.ToString().PadLeft(15, '-'),
            });

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData(49)]
        [InlineData(48)]
        [InlineData(01)]
        [InlineData(00)]
        public void Given_destination_shorter_than_max_position_especified_then_chars_written_should_be_zero(int destinationSize)
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 15, paddingChar: ' ')
                .Skip(1)
                .Map(x => x.Birthday, 10, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Money, 7, precision: 2, padding: Padding.Left, paddingChar: '0')
                .Skip(1)
                .Map(x => x.Color, 15, padding: Padding.Left, paddingChar: '-')
                .Build();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 0123.456M,
                            Color: Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Given_string_value_larger_than_designated_space_should_write_until_reach_it()
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(DateTime Birthday, string Name)>()
                .Map(x => x.Birthday, 10, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Name, 10)
                .Build();

            var instance = (Birthday: new DateTime(2020, 05, 23),
                            Name: "foo bar baz");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);

            result.Should().Be("2020.05.23");
        }

        [Fact]
        public void Given_non_string_value_larger_than_designated_space_should_write_until_reach_it()
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday)>()
                .Map(x => x.Name, 15, paddingChar: '-')
                .Skip(1)
                .Map(x => x.Birthday, 9, "yyyy.MM.dd")
                .Build();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23));

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);

            result.Should().Be("foo bar baz----");
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance, 12, padding: Padding.Left, paddingChar: '0')
                .Skip(1)
                .Map(x => x.Date, 8)
                .Skip(1)
                .Map(x => x.Debit, 6, padding: Padding.Left, paddingChar: '0')
                .DefaultTypeConvert<decimal>((span, value) => (((long)(value * 100)).TryFormat(span, out var written), written))
                .DefaultTypeConvert<DateTime>((span, value) => (value.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Build();

            var instance = (Balance: 0123456789.01M,
                            Date: new DateTime(2020, 05, 23),
                            Debit: 0123.45M);

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("012345678901\023052020\0012345");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            // Arrange

            var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 12, (span, text) => (true, text.AsSpan().ToUpperInvariant(span)))
                .Map(x => x.Birthday, 8, (span, date) => (date.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Skip(2)
                .Map(x => x.Money, 7, padding: Padding.Left, paddingChar: '0')
                .Skip(1)
                .Map(x => x.Nickname, 8, (span, text) => (true, text.AsSpan().Slice(0, 4).ToUpperInvariant(span)), paddingChar: '-')
                .Build();

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 123.45M,
                            Nickname: "nick name");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("FOO BAR BAZ 23052020\0\00123.45\0NICK----");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            // Assert

            var writer = new FixedLengthWriterSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 3, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(x => x.MotherAge, 3)
                .Map(x => x.FatherAge, 3)
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build();

            var instance = (Age: 15,
                            MotherAge: 40,
                            FatherAge: 50);

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("30 42 52 ");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_non_member_expression_on_mapping_should_parse()
        {
            // Assert

            var called = 0;
            var (age, motherAge, fatherAge) = (15, 40, new Func<int>(() => { called++; return 50; }));

            var writer = new FixedLengthWriterSequentialBuilder<bool>()
                .Map(_ => age, 3, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(_ => motherAge, 3)
                .Map(_ => fatherAge(), 3)
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build();

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(default, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("30 42 52 ");
            unwritted.Should().Be(new string(default, freeSpace));

            called.Should().Be(1);
        }

        [Fact]
        public void Given_nested_mapped_property_should_create_nested_instance_to_parse()
        {
            // Arrange 

            var writer = new FixedLengthWriterSequentialBuilder<Person>()
                .Map(x => x.BirthDay, 10, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Name, 8)
                .Skip(1)
                .Map(x => x.Mother.BirthDay, 10, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Mother.Name, 11)
                .Build();

            var instance = new Person
            {
                BirthDay = new DateTime(2020, 05, 23),
                Name = "son name",
                Mother = new Person
                {
                    BirthDay = new DateTime(1980, 01, 15),
                    Name = "mother name",
                }
            };

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("2020.05.23\0son name\01980.01.15\0mother name");
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }

    public static class FixedLengthWriterSequentialHelpers
    {
        public static IFixedLengthWriterSequentialBuilder<T> Map<T>(this IFixedLengthWriterSequentialBuilder<T> builder, Expression<Func<T, decimal>> ex, int length, int precision, string format = null, Padding padding = Padding.Right, char paddingChar = ' ')
        {
            var multiply = (int)Math.Pow(10, precision);

            return builder.Map(ex, length,
                (span, value) => (((int)(value * multiply)).TryFormat(span, out var written, format), written),
                padding, paddingChar);
        }
    }
}
