using FluentAssertions;
using RecordParser.Builders.Reader;
using RecordParser.Parsers;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthReaderSequentialBuilderTest : TestSetup
    {
        [Fact]
        public void Given_factory_method_should_invoke_it_on_parse()
        {
            var called = 0;
            var date = new DateTime(2020, 05, 23);
            var color = Color.LightBlue;

            var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name)
                .Skip(1)
                .Map(x => x.Money)
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
            var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name)
                .Map(x => x.Birthday)
                .Map(x => x.Money)
                .Build(";");

            var result = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_columns_to_ignore_and_value_using_standard_format_should_parse_without_extra_configuration()
        {
            var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name)
                .Skip(1)
                .Map(x => x.Birthday)
                .Skip(2)
                .Map(x => x.Money)
                .Build(";");

            var result = reader.Parse("foo bar baz ; IGNORE; 2020.05.23 ; IGNORE ; IGNORE ; 0123.45");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            var reader = new VariableLengthReaderSequentialBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance)
                .Map(x => x.Date)
                .Map(x => x.Debit)
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
            var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, value => value.ToUpper())
                .Map(x => x.Birthday, value => DateTime.ParseExact(value, "ddMMyyyy", null))
                .Map(x => x.Money)
                .Map(x => x.Nickname, value => value.Slice(0, 4).ToString())
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
            var reader = new VariableLengthReaderSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, value => int.Parse(value) * 2)
                .Map(x => x.MotherAge)
                .Map(x => x.FatherAge)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build(";");

            var result = reader.Parse(" 15 ; 40 ; 50 ");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Given_columns_to_ignore_and_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            var reader = new VariableLengthReaderSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Skip(2)
                .Map(x => x.MotherAge)
                .Skip(1)
                .Map(x => x.Age, value => int.Parse(value) * 2)
                .Map(x => x.FatherAge)
                .DefaultTypeConvert(value => int.Parse(value) + 2)
                .Build(";");

            var result = reader.Parse(" XX ; XX ; 40 ; XX ; 15 ; 50 ; XX");

            result.Should().BeEquivalentTo((Age: 30,
                                            MotherAge: 42,
                                            FatherAge: 52));
        }

        [Fact]
        public void Custom_format_configurations_can_be_simplified_with_user_defined_extension_methods()
        {
            var reader = new VariableLengthReaderSequentialBuilder<(string Name, decimal Balance, DateTime Date)>()
                .Map(x => x.Balance)
                .MyMap(x => x.Date, format: "ddMMyyyy")
                .Map(x => x.Name)
                .MyBuild();

            var result = reader.Parse("012345678.901 ; 23052020 ; FOOBAR ");

            result.Should().BeEquivalentTo((Name: "foobar",
                                            Balance: 012345678.901M,
                                            Date: new DateTime(2020, 05, 23)));
        }

        [Fact]
        public void Given_non_member_expression_on_mapping_should_parse()
        {
            (string name, DateTime birthday, decimal money, Color color) = (default, default, default, default);

            var reader = new VariableLengthReaderSequentialBuilder<bool>()
                .Map(_ => name)
                .Map(_ => birthday)
                .Map(_ => money)
                .Map(_ => color)
                .Build(";");

            _ = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45; LightBlue");

            var result = (name, birthday, money, color);

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M,
                                            Color: Color.LightBlue));
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

            var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                 .Map(x => x.Name)
                 .Map(x => x.Birthday)
                 .Map(x => x.Money)
                 .Map(x => x.Color)
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
        [InlineData("pt-BR")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("ru-RU")]
        [InlineData("es-ES")]
        public void Registered_primitives_types_should_have_default_converters_which_uses_current_cultureinfo(string cultureName)
        {
            var expected = new AllType
            {
                Str = "Foo Bar",
                Char = 'z',

                Byte = 42,
                SByte = -43,

                Double = -1.58D,
                Float = 1.46F,

                Int = -6,
                UInt = 7,

                Long = -3,
                ULong = 45,

                Short = -2,
                UShort = 8,

                Guid = new Guid("e808927a-48f9-4402-ab2b-400bf1658169"),
                Date = DateTime.Parse(DateTime.Now.ToString()),
                TimeSpan = DateTime.Now.TimeOfDay,

                Bool = true,
                Decimal = -1.99M,
            };

            var reader = new VariableLengthReaderSequentialBuilder<AllType>()
            .Map(x => x.Str)
            .Map(x => x.Char)

            .Map(x => x.Byte)
            .Map(x => x.SByte)

            .Map(x => x.Double)
            .Map(x => x.Float)

            .Map(x => x.Int)
            .Map(x => x.UInt)

            .Map(x => x.Long)
            .Map(x => x.ULong)

            .Map(x => x.Short)
            .Map(x => x.UShort)

            .Map(x => x.Guid)
            .Map(x => x.Date)
            .Map(x => x.TimeSpan)

            .Map(x => x.Bool)
            .Map(x => x.Decimal)

            .Build(";");

            var values = new object[]
            {
                expected.Str,
                expected.Char,

                expected.Byte,
                expected.SByte,

                expected.Double,
                expected.Float,

                expected.Int,
                expected.UInt,

                expected.Long,
                expected.ULong,

                expected.Short,
                expected.UShort,

                expected.Guid,
                expected.Date,
                expected.TimeSpan,

                expected.Bool,
                expected.Decimal,
            };

            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            var line = string.Join(';', values.Select(x => $"  {x}  "));

            var result = reader.Parse(line);

            result.Should().BeEquivalentTo(expected);
        }
    }

    public static class CSVSequentialBuilderExtensions
    {
        public static IVariableLengthReaderSequentialBuilder<T> MyMap<T>(
            this IVariableLengthReaderSequentialBuilder<T> source,
            Expression<Func<T, DateTime>> ex, 
            string format)
        {
            return source.Map(ex, value => DateTime.ParseExact(value, format, null));
        }

        public static IVariableLengthReader<T> MyBuild<T>(this IVariableLengthReaderSequentialBuilder<T> source)
        {
            return source.DefaultTypeConvert(value => value.ToLower())
                         .Build(";");
        }
    }
}
