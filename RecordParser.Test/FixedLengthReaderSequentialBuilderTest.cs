using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthReaderSequentialBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new FixedLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, length: 11)
                .Skip(1)
                .Map(x => x.Birthday, 10)
                .Skip(1)
                .Map(x => x.Money, 7)
                .Build();

            var result = reader.Parse("foo bar baz 2020.05.23 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new FixedLengthReaderSequentialBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 12)
                .Skip(1)
                .Map(x => x.Date, 8)
                .Skip(1)
                .Map(x => x.Debit, 5)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build();

            var result = reader.Parse("012345678901 23052020 12345");

            result.Should().BeEquivalentTo((Debit: 123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new FixedLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 12, value => value.ToUpper())
                .Map(x => x.Birthday, 8, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Skip(1)
                .Map(x => x.Money, 7)
                .Map(x => x.Nickname, 8, value => value.First() + "." + value.Last())
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
            var reader = new FixedLengthReaderSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 4, value => int.Parse(value) * 2)
                .Map(x => x.MotherAge, 4)
                .Map(x => x.FatherAge, 4)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build();

            var result = reader.Parse(" 15  40  50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Given_trim_is_enabled_should_remove_whitespace_from_both_sides_of_string()
        {
            var reader = new FixedLengthReaderSequentialBuilder<(string Foo, string Bar, string Baz)>()
                .Map(x => x.Foo, 4)
                .Map(x => x.Bar, 4)
                .Map(x => x.Baz, 4)
                .Build();

            var result = reader.Parse(" foo bar baz ");

            result.Should().BeEquivalentTo((Foo: "foo",
                                            Bar: "bar",
                                            Baz: "baz"));
        }
    }
}
