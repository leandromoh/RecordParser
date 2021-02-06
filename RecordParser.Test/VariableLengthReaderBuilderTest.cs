using FluentAssertions;
using RecordParser.Parsers;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthReaderBuilderTest
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColum: 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(";");

            var result = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45; LightBlue");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M,
                                            Color: Color.LightBlue));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new VariableLengthReaderBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Debit, 2)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build(";");

            var result = reader.Parse("012345678901 ; 23052020 ; 012345");

            result.Should().BeEquivalentTo((Debit: 0123.45M,
                                            Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_some_values_with_custom_format_should_allow_define_custom_parser_for_member()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, value => value.ToUpper())
                .Map(x => x.Birthday, 1, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money, 2)
                .Map(x => x.Nickname, 3, value => value.First() + "." + value.Last())
                .Build(";");

            var result = reader.Parse("foo bar baz ; 23052020 ; 012345 ; nickname");

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "n.e"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new VariableLengthReaderBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0)
                .Map(x => x.MotherAge, 1, value => int.Parse(value) + 3)
                .Map(x => x.FatherAge, 2)
                .Build(";");

            var result = reader.Parse(" 15 ; 40 ; 50 ");

            result.Should().BeEquivalentTo((Age: 15,
                                            MotherAge: 43,
                                            FatherAge: 50));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Name, 2)
                .MyMap(x => x.Date, 1, format: "ddMMyyyy")
                .MyBuild();

            var result = reader.Parse("012345678.901 ; 23052020 ; FOOBAR ");

            result.Should().BeEquivalentTo((Name: "foobar",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_nested_mapped_property_should_create_nested_instance_to_parse()
        {
            var reader = new VariableLengthReaderBuilder<Person>()
                .Map(x => x.BirthDay, 0)
                .Map(x => x.Name, 1)
                .Map(x => x.Mother.BirthDay, 2)
                .Map(x => x.Mother.Name, 3)
                .Build(";");

            var result = reader.Parse("2020.05.23 ; son name ; 1980.01.15; mother name");

            result.Should().BeEquivalentTo(new Person
            {
                BirthDay = new DateTime(2020, 05, 23),
                Name = "son name",
                Mother = new Person
                {
                    BirthDay = new DateTime(1980, 01, 15),
                    Name = "mother name",
                }
            });
        }

        [Fact]
        public void Given_invalid_record_called_with_try_parse_should_not_throw()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1)
                .Build(";");

            var parsed = reader.TryParse(" foo ; datehere", out var result);

            parsed.Should().BeFalse();
            result.Should().Be(default);
        }

        [Fact]
        public void Given_valid_record_called_with_try_parse_should_set_out_parameter_with_result()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColum: 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(";");

            var parsed = reader.TryParse("foo bar baz ; 2020.05.23 ; 0123.45; LightBlue", out var result);

            parsed.Should().BeTrue();
            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M,
                                            Color: Color.LightBlue));
        }
    }

    public static class VariableLengthReaderCustomExtensions
    {
        public static IVariableLengthReaderBuilder<T> MyMap<T>(
            this IVariableLengthReaderBuilder<T> source,
            Expression<Func<T, DateTime>> ex, int startIndex,
            string format)
        {
            return source.Map(ex, startIndex, value => DateTime.ParseExact(value, format, null));
        }

        public static IVariableLengthReader<T> MyBuild<T>(this IVariableLengthReaderBuilder<T> source)
        {
            return source.DefaultTypeConvert(value => value.ToLower())
                         .Build(";");
        }
    }
}
