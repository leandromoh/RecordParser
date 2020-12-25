﻿using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class SpanSpanCSVIndexedBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new SpanCSVIndexedBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Build();

            var result = reader.Parse("foo bar baz ; 20200523 ; 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new SpanCSVIndexedBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Debit, 2)
                .DefaultTypeConvert(value => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, new[] { "dd.MM.yyyy" }, null, DateTimeStyles.AllowWhiteSpaces))
                .Build();

            var result = reader.Parse("012345678901 ; 23.05.2020 ; 012345");

            result.Should().BeEquivalentTo((Debit: 0123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new SpanCSVIndexedBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, value => new string(value))
                .Map(x => x.Birthday, 1, value => DateTime.ParseExact(value, new[] { "ddMMyyyy" }, null, DateTimeStyles.AllowWhiteSpaces))
                .Map(x => x.Money, 2)
                .Map(x => x.Nickname, 3)
                .Build();

            var result = reader.Parse("foo bar baz ; 23052020 ; 012345 ; nickname");

            result.Should().BeEquivalentTo((Name: "foo bar baz ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: " nickname"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new SpanCSVIndexedBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 1, value => int.Parse(value, NumberStyles.Integer, null) * 2)
                .Map(x => x.MotherAge, 0)
                .Map(x => x.FatherAge, 2)
                .DefaultTypeConvert(value => int.Parse(value, NumberStyles.Integer, null) + 2)
                .Build();

            var result = reader.Parse(" 40 ; 15 ; 50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new SpanCSVIndexedBuilder<(string Name, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Name, 2)
                .MyMap(x => x.Date, 1, format: "ddMMyyyy")
                .Build();

            var result = reader.Parse("012345678.901 ; 23052020 ; FOOBAR ");

            result.Should().BeEquivalentTo((Name: " FOOBAR ",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }
    }

    public static class SpanCSVCustomExtensions
    {
        public static ISpanCSVIndexedBuilder<T> MyMap<T>(
            this ISpanCSVIndexedBuilder<T> source,
            Expression<Func<T, DateTime>> ex, int startIndex,
            string format)
        {
            return source.Map(ex, startIndex, value => DateTime.ParseExact(value, new[] { format }, null, DateTimeStyles.AllowWhiteSpaces));
        }
    }
}
