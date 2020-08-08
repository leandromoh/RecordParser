﻿using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class CSVSequentialBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new CSVSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name)
                .Map(x => x.Birthday)
                .Map(x => x.Money)
                .Build();

            var result = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_columns_to_ignore_and_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new CSVSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name)
                .Skip(1)
                .Map(x => x.Birthday)
                .Skip(2)
                .Map(x => x.Money)
                .Build();

            var result = reader.Parse("foo bar baz ; IGNORE; 2020.05.23 ; IGNORE ; IGNORE ; 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new CSVSequentialBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance)
                .Map(x => x.Date)
                .Map(x => x.Debit)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build();

            var result = reader.Parse("012345678901 ; 23052020 ; 012345");

            result.Should().BeEquivalentTo((Debit: 0123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new CSVSequentialBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, value => value.ToUpper())
                .Map(x => x.Birthday, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money)
                .Map(x => x.Nickname, value => value.First() + "." + value.Last())
                .Build();

            var result = reader.Parse("foo bar baz ; 23052020 ; 012345 ; nickname");

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "n.e"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new CSVSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.MotherAge)
                .Map(x => x.Age, value => int.Parse(value) * 2)
                .Map(x => x.FatherAge)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build();

            var result = reader.Parse(" 40 ; 15 ; 50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Given_columns_to_ignore_and_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new CSVSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Skip(2)
                .Map(x => x.MotherAge)
                .Skip(1)
                .Map(x => x.Age, value => int.Parse(value) * 2)
                .Map(x => x.FatherAge)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build();

            var result = reader.Parse(" XX ; XX ; 40 ; XX ; 15 ; 50 ; XX");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new CSVSequentialBuilder<(string Name, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance)
                .MyMap(x => x.Date, format: "ddMMyyyy")
                .Map(x => x.Name)
                .MyBuild();

            var result = reader.Parse("012345678.901 ; 23052020 ; FOOBAR ");

            result.Should().BeEquivalentTo((Name: "foobar",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }
    }

    public static class CSVSequentialBuilderExtensions
    {
        public static ICSVSequentialBuilder<T> MyMap<T>(
            this ICSVSequentialBuilder<T> source,
            Expression<Func<T, DateTime>> ex, 
            string format)
        {
            return source.Map(ex, value => DateTime.ParseExact(value, format, null));
        }

        public static ICSVReader<T> MyBuild<T>(this ICSVSequentialBuilder<T> source)
        {
            return source.DefaultTypeConvert(value => value.ToLower())
                         .Build();
        }
    }
}