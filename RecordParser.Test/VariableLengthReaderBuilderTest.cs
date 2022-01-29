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
    public partial class VariableLengthReaderBuilderTest : TestSetup
    {
        [Fact]
        public void Given_factory_method_should_invoke_it_on_parse()
        {
            var called = 0;
            var date = new DateTime(2020, 05, 23);
            var color = Color.LightBlue;

            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 0)
                .Map(x => x.Money, 2)
                .Build(";", factory: () => { called++; return (default, date, default, color); });

            var result = reader.Parse("foo bar baz ; yyyy.MM.dd ; 0123.45; IGNORE ");

            called.Should().Be(1);

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: date,
                                            Money: 123.45M,
                                            Color: color));
        }

        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 0)
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
        public void Given_non_member_expression_on_mapping_should_parse()
        {
            (string name, DateTime birthday, decimal money, Color color) = (default, default, default, default);

            var reader = new VariableLengthReaderBuilder<bool>()
                .Map(_ => name, indexColumn: 0)
                .Map(_ => birthday, 1)
                .Map(_ => money, 2)
                .Map(_ => color, 3)
                .Build(";");

            _ = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45; LightBlue");

            var result = (name, birthday, money, color);

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M,
                                            Color: Color.LightBlue));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new VariableLengthReaderBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Debit, 2)
                .DefaultTypeConvert(value => decimal.Parse(value) / 100)
                .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Build(";");

            var result = reader.Parse("012345678901 ; 23052020 ; 012345");

            result.Should().BeEquivalentTo((Balance: 0123456789.01M,
                                            Date: new DateTime(2020, 05, 23),
                                            Debit: 0123.45M));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 0, value => value.ToUpper())
                .Map(x => x.Birthday, 1, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money, 2)
                .Map(x => x.Nickname, 3, value => value.Slice(0, 4).ToString())
                .Build(";");

            var result = reader.Parse("foo bar baz ; 23052020 ; 012345 ; nickname");

            result.Should().BeEquivalentTo((Name: "FOO BAR BAZ",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 12345M,
                                            Nickname: "nick"));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new VariableLengthReaderBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0, value => int.Parse(value) * 2)
                .Map(x => x.MotherAge, 1)
                .Map(x => x.FatherAge, 2)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build(";");

            var result = reader.Parse(" 15 ; 40 ; 50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
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
        public void Given_trim_is_enabled_should_remove_whitespace_from_both_sides_of_string()
        {
            var reader = new VariableLengthReaderBuilder<(string Foo, string Bar, string Baz)>()
                .Map(x => x.Foo, 0)
                .Map(x => x.Bar, 1)
                .Map(x => x.Baz, 2)
                .Build(";");

            var result = reader.Parse(" foo ; bar ; baz ");

            result.Should().BeEquivalentTo((Foo: "foo",
                                            Bar: "bar",
                                            Baz: "baz"));
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
                .Map(x => x.Name, indexColumn: 0)
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

        [Theory]
        [InlineData(" ")]
        [InlineData("2020.05.23")]
        public void Given_record_with_nullable_struct_field_should_parse_properly(string birthday)
        {
            var reader = new VariableLengthReaderBuilder<(string Name, DateTime? Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(";");

            var expectedBirthday = DateTime.TryParse(birthday, out var date)
                                    ? date
                                    : (DateTime?) null;

            var parsed = reader.TryParse($"foo bar baz ; {birthday} ; 0123.45; LightBlue", out var result);

            parsed.Should().BeTrue();
            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: expectedBirthday,
                                            Money: 123.45M,
                                            Color: Color.LightBlue));
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

            var result = reader.Parse("2020.05.23 ; son name ; 1980.01.15 ; mother name");

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

            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                 .Map(x => x.Name, 0)
                 .Map(x => x.Birthday, 1)
                 .Map(x => x.Money, 2)
                 .Map(x => x.Color, 3)
                 .Build(";", culture);

            var values = new[]
            {
                expected.Name.ToString(culture),
                expected.Birthday.ToString(culture),
                expected.Money.ToString(culture),
                expected.Color.ToString(),
            };

            var line = string.Join(';', values.Select(x => $"  {x}  "));

            var result = reader.Parse(line);

            result.Should().BeEquivalentTo(expected);
        }

        [Theory]
        // text as is
        [InlineData("Black", Color.Black)]
        // text uppercase
        [InlineData("WHITE", Color.White)]
        // text lowercase
        [InlineData("yellow", Color.Yellow)]
        // numeric value present in enum
        [InlineData("3", Color.LightBlue)]
        // numeric value NOT present in enum
        [InlineData("777", (Color)777)]
        public void Parse_enum_same_way_framework(string text, Color expected)
        {
            var reader = new VariableLengthReaderBuilder<Color>()
                .Map(x => x, 0)
                .Build(";");

            reader.Parse(text).Should().Be(expected);
        }

        [Theory]
        [InlineData(FlaggedEnum.Some)]
        [InlineData(FlaggedEnum.Another)]
        [InlineData(FlaggedEnum.Other | FlaggedEnum.Some)]
        [InlineData(FlaggedEnum.None | FlaggedEnum.Another)]
        [InlineData((FlaggedEnum)777)]
        public void Parse_flag_enum_same_way_framework(FlaggedEnum expected)
        {
            var reader = new VariableLengthReaderBuilder<FlaggedEnum>()
                .Map(x => x, 0)
                .Build(";");

            var text = expected.ToString();

            reader.Parse(text).Should().Be(expected);
        }

        [Fact]
        public void Parse_enum_with_text_not_present_in_enum_should_be_same_way_framework()
        {
            var reader = new VariableLengthReaderBuilder<Color>()
                .Map(x => x, 0)
                .Build(";");

            var actualEx = AssertionExtensions.Should(() => reader.Parse("foo")).Throw<ArgumentException>().Which;
            var expectedEx = AssertionExtensions.Should(() => Enum.Parse<Color>("foo")).Throw<ArgumentException>().Which;

            actualEx.Should().BeEquivalentTo(expectedEx, cfg => cfg.Excluding(x => x.StackTrace));
        }

        [Fact]
        public void Given_empty_enum_should_parse_same_way_framework()
        {
            var reader = new VariableLengthReaderBuilder<EmptyEnum>()
                .Map(x => x, 0)
                .Build(";");

            reader.Parse("777").Should().Be((EmptyEnum)777);
        }

        [Fact]
        public void Given_variable_length_reader_used_in_multi_thread_context_parse_method_should_be_thread_safe()
        {
            // Arrange 

            var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1)
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var lines = new Fixture()
                .CreateMany<(string Name, DateTime Birthday, decimal Money, Color Color)>(10_000)
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
