using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthReaderBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, 0, 12)
                .Map(x => x.Birthday, 12, 10)
                .Map(x => x.Money, 23, 7)
                .Build();

            var result = reader.Parse("foo bar baz 2020.05.23 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz ",
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
                .DefaultConvert(value => decimal.Parse(value) / 100)
                .DefaultConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
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

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ ",
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
                .DefaultConvert(value => int.Parse(value) + 2)
                .Build();

            var result = reader.Parse(" 15  40  50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }
    }
}
