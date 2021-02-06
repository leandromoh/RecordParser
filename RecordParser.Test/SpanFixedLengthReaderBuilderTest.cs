using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class SpanFixedLengthReaderBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, 0, 12)
                .Map(x => x.Birthday, 12, 10)
                .Map(x => x.Money, 23, 7)
                .Build();

            var result = reader.Parse("foo bar baz 2020.05.23 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new SpanFixedLengthReaderBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0, 12)
                .Map(x => x.Date, 13, 10)
                .Map(x => x.Debit, 24, 6)
                .DefaultTypeConvert(value => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, new[] { "dd.MM.yyyy" }, null, DateTimeStyles.None))
                .Build();

            var result = reader.Parse("012345678901 23.05.2020 012345");

            result.Should().BeEquivalentTo((Debit: 0123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, 12, value => new string(value))
                .Map(x => x.Birthday, 12, 8, value => DateTime.ParseExact(value, "ddMMyyyy", null, DateTimeStyles.None))
                .Map(x => x.Money, 21, 7)
                .Map(x => x.Nickname, 28, 8)
                .Build();

            var result = reader.Parse("foo bar baz 23052020 012345 nickname");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "nickname"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new SpanFixedLengthReaderBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0, 4, value => int.Parse(value, NumberStyles.Integer, null) * 2)
                .Map(x => x.MotherAge, 4, 4)
                .Map(x => x.FatherAge, 8, 4)
                .Build();

            var result = reader.Parse(" 15  40  50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 40,
                                            FatherAge: 50));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Name, decimal Balance, DateTime Date)>()
                .MyMap(x => x.Balance, 0, 12, decimalPlaces: 3)
                .MyMap(x => x.Date, 13, 8, format: "ddMMyyyy")
                .Map(x => x.Name, 22, 7)
                .Build();

            var result = reader.Parse("012345678901 23052020 FOOBAR ");

            result.Should().BeEquivalentTo((Name: "FOOBAR",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_trim_is_enabled_should_remove_whitespace_from_both_sides_of_string()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Foo, string Bar, string Baz)>()
                .Map(x => x.Foo, 0, 5)
                .Map(x => x.Bar, 4, 5)
                .Map(x => x.Baz, 8, 5)
                .Build();

            var result = reader.Parse(" foo bar baz ");

            result.Should().BeEquivalentTo((Foo: "foo",
                                            Bar: "bar",
                                            Baz: "baz"));
        }

        [Fact]
        public void Given_invalid_record_called_with_try_parse_should_not_throw()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Name, DateTime Birthday)>()
                .Map(x => x.Name, 0, 5)
                .Map(x => x.Birthday, 5, 10)
                .Build();

            var parsed = reader.TryParse(" foo datehere", out var result);

            parsed.Should().BeFalse();
            result.Should().Be(default);
        }

        [Fact]
        public void Given_valid_record_called_with_try_parse_should_set_out_parameter_with_result()
        {
            var reader = new SpanFixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, 0, 12)
                .Map(x => x.Birthday, 12, 10)
                .Map(x => x.Money, 23, 7)
                .Build();

            var parsed = reader.TryParse("foo bar baz 2020.05.23 0123.45", out var result);

            parsed.Should().BeTrue();
            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }
    }

    public static class SpanFixedLengthCustomExtensions
    {
        public static ISpanFixedLengthReaderBuilder<T> MyMap<T>(
            this ISpanFixedLengthReaderBuilder<T> source,
            Expression<Func<T, DateTime>> ex, int startIndex, int length,
            string format)
        {
            return source.Map(ex, startIndex, length, value => DateTime.ParseExact(value, new[] { format }, null, DateTimeStyles.None));
        }

        public static ISpanFixedLengthReaderBuilder<T> MyMap<T>(
            this ISpanFixedLengthReaderBuilder<T> source,
            Expression<Func<T, decimal>> ex, int startIndex, int length,
            int decimalPlaces)
        {
            return source.Map(ex, startIndex, length, value => decimal.Parse(value, NumberStyles.Number, null) / (decimal)Math.Pow(10, decimalPlaces));
        }
    }
}
