using AutoFixture;
using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Builders.Writer;
using RecordParser.Parsers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthReaderBuilderTest : TestSetup
    {
        [Fact]
        public void Given_factory_method_should_invoke_it_on_parse()
        {
            var called = 0;
            var date = new DateTime(2020, 05, 23);
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, startIndex: 0, length: 11)
                .Map(x => x.Money, 23, 7)
                .Build(factory: () => { called++; return (default, date, default); });

            var result = reader.Parse("foo bar baz yyyy.MM.dd 0123.45");

            called.Should().Be(1);

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: date,
                                            Money: 123.45M));
        }

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
            var reader = new FixedLengthReaderBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance, 0, 12)
                .Map(x => x.Date, 13, 8)
                .Map(x => x.Debit, 22, 6)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build();

            var result = reader.Parse("012345678901 23052020 012345");

            result.Should().BeEquivalentTo((Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23),
                                            Debit: 123.45M));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, 12, value => value.ToUpper())
                .Map(x => x.Birthday, 12, 8, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money, 21, 7)
                .Map(x => x.Nickname, 28, 8, value => value.Slice(0, 4).ToString())
                .Build();

            var result = reader.Parse("foo bar baz 23052020 012345 nickname");

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "nick"));
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


        [Fact]
        public void Given_nested_mapped_property_should_create_nested_instance_to_parse()
        {
            var reader = new FixedLengthReaderBuilder<Person>()
                .Map(x => x.BirthDay, 0, 10)
                .Map(x => x.Name, 10, 10)
                .Map(x => x.Mother.BirthDay, 20, 10)
                .Map(x => x.Mother.Name, 30, 12)
                .Build();

            var result = reader.Parse("2020.05.23 son name 1980.01.15 mother name");

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
        public void Given_non_member_expression_on_mapping_should_parse()
        {
            (string name, DateTime birthday, decimal money) = (default, default, default);

            var reader = new FixedLengthReaderBuilder<bool>()
                .Map(_ => name, startIndex: 0, length: 11)
                .Map(_ => birthday, 12, 10)
                .Map(_ => money, 23, 7)
                .Build();

            _ = reader.Parse("foo bar baz 2020.05.23 0123.45");

            var result = (name, birthday, money);

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Theory]
        [InlineData("pt-BR")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("ru-RU")]
        [InlineData("es-ES")]
        public void Builder_should_use_passed_cultureinfo_to_parse_record(string cultureName)
        {
            var culture = new CultureInfo(cultureName);

            var expected = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 123.45M,
                            Color: Color.LightBlue);

            const int length = 25;

            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                 .Map(x => x.Name, 0, length)
                 .Map(x => x.Birthday, 25, length)
                 .Map(x => x.Money, 50, length)
                 .Map(x => x.Color, 75, length)
                 .Build(culture);

            var values = new[]
            {
                expected.Name.ToString(culture).PadRight(length),
                expected.Birthday.ToString(culture).PadRight(length),
                expected.Money.ToString(culture).PadRight(length),
                expected.Color.ToString().PadRight(length),
            };

            var line = string.Join(string.Empty, values);

            var result = reader.Parse(line);

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Given_fixed_length_reader_used_in_multi_thread_context_parse_method_should_be_thread_safe()
        {
            // Arrange 

            var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, 15)
                .Map(x => x.Birthday, 16, 10)
                .Map(x => x.Money, 27, 7)
                .Map(x => x.Color, 35, 15)
                .Build();

            var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, 15, paddingChar: ' ')
                .Map(x => x.Birthday, 16, 10, "yyyy.MM.dd")
                .Map(x => x.Money, 27, 7, precision: 2)
                .Map(x => x.Color, 35, 15, padding: Padding.Right)
                .Build();

            var lines = new Fixture()
                .Build<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .With(x => x.Name, () => Guid.NewGuid().ToString().Substring(0, 14))
                .CreateMany(10_000)
                .Select(item => 
                {
                    Span<char> destination = stackalloc char[100];
                    var success = writer.TryFormat(item, destination, out var charsWritten);
                    Debug.Assert(success);
                    var line = destination.Slice(0, charsWritten).ToString();
                    return line;
                })
                .ToList();

            // Act

            var resultParallel = lines
                .AsParallel()
                .Select(line => reader.Parse(line))
                .ToList();

            var resultSequential = lines
                .Select(line => reader.Parse(line))
                .ToList();

            // Assert

            resultParallel.Should().BeEquivalentTo(resultSequential, cfg => cfg.WithStrictOrdering());
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
