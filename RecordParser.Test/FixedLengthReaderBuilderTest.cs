using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthReaderBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, startIndex: 0, length: 11)
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
            var reader = new FixedLengthReaderBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0, 12)
                .Map(x => x.Date, 13, 8)
                .Map(x => x.Debit, 22, 6)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build();

            var result = reader.Parse("012345678901 23052020 012345");

            result.Should().BeEquivalentTo((Debit: 0123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, 12, value => value.ToUpper())
                .Map(x => x.Birthday, 12, 8, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money, 21, 7)
                .Map(x => x.Nickname, 28, 8, value => value.First() + "." + value.Last())
                .Build();

            var result = reader.Parse("foo bar baz 23052020 012345 nickname");

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "n.e"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new FixedLengthReaderBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0, 4, value => int.Parse(value) * 2)
                .Map(x => x.MotherAge, 4, 4)
                .Map(x => x.FatherAge, 8, 4)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build();

            var result = reader.Parse(" 15  40  50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new FixedLengthReaderBuilder<(string Name, decimal Balance, DateTime Date)>()
                .MyMap(x => x.Balance, 0, 12, decimalPlaces: 3)
                .MyMap(x => x.Date, 13, 8, format: "ddMMyyyy")
                .Map(x => x.Name, 22, 7)
                .MyBuild();

            var result = reader.Parse("012345678901 23052020 FOOBAR ");

            result.Should().BeEquivalentTo((Name: "foobar",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_trim_is_enabled_should_remove_whitespace_from_both_sides_of_string()
        {
            var reader = new FixedLengthReaderBuilder<(string Foo, string Bar, string Baz)>()
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
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday)>()
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
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, startIndex: 0, length: 11)
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

    public static class FixedLengthCustomExtensions
    {
        public static IFixedLengthReaderBuilder<T> MyMap<T>(
            this IFixedLengthReaderBuilder<T> source,
            Expression<Func<T, DateTime>> ex, int startIndex, int length,
            string format)
        {
            return source.Map(ex, startIndex, length, value => DateTime.ParseExact(value, format, null));
        }

        public static IFixedLengthReaderBuilder<T> MyMap<T>(
            this IFixedLengthReaderBuilder<T> source,
            Expression<Func<T, decimal>> ex, int startIndex, int length,
            int decimalPlaces)
        {
            return source.Map(ex, startIndex, length, value => decimal.Parse(value) / (decimal)Math.Pow(10, decimalPlaces));
        }

        public static IFixedLengthReader<T> MyBuild<T>(this IFixedLengthReaderBuilder<T> source)
        {
            return source.DefaultTypeConvert(value => value.ToLower())
                         .Build();
        }
    }
}
