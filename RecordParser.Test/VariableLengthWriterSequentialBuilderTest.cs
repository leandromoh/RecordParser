using FluentAssertions;
using RecordParser.Builders.Writer;
using RecordParser.Test;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthWriterSequentialBuilderTest : TestSetup
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name)
                .Map(x => x.Birthday, "yyyy.MM.dd")
                .Map(x => x.Money)
                .Map(x => x.Color)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name)
                .Map(x => x.Birthday, "yyyy.MM.dd")
                .Skip(2)
                .Map(x => x.Color)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo bar baz ; 2020.05.23 ;  ;  ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Skip(1)
                .Map(x => x.Name)
                .Map(x => x.Birthday, "yyyy.MM.dd")
                .Map(x => x.Money)
                .Map(x => x.Color)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip2_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Skip(1)
                .Map(x => x.Name)
                .Map(x => x.Birthday, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Money)
                .Map(x => x.Color)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData(99, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(52, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]
        [InlineData(51, true, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; LightBlue")]

        [InlineData(50, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(43, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]
        [InlineData(42, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45 ; ")]

        [InlineData(41, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(40, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]
        [InlineData(39, false, " ; foo bar baz ; 2020.05.23 ;  ; 123.45")]

        [InlineData(38, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(34, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]
        [InlineData(33, false, " ; foo bar baz ; 2020.05.23 ;  ; ")]

        [InlineData(32, false, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(31, false, " ; foo bar baz ; 2020.05.23 ; ")]
        [InlineData(30, false, " ; foo bar baz ; 2020.05.23 ; ")]

        [InlineData(29, false, " ; foo bar baz ; 2020.05.23")]
        [InlineData(28, false, " ; foo bar baz ; 2020.05.23")]
        [InlineData(27, false, " ; foo bar baz ; 2020.05.23")]

        [InlineData(26, false, " ; foo bar baz ; ")]
        [InlineData(18, false, " ; foo bar baz ; ")]
        [InlineData(17, false, " ; foo bar baz ; ")]

        [InlineData(16, false, " ; foo bar baz")]
        [InlineData(15, false, " ; foo bar baz")]
        [InlineData(14, false, " ; foo bar baz")]

        [InlineData(13, false, " ; ")]
        [InlineData(04, false, " ; ")]
        [InlineData(03, false, " ; ")]

        [InlineData(02, false, "")]
        [InlineData(01, false, "")]
        [InlineData(00, false, "")]
        public void Given_too_short_destination_should_write_while_have_enough_space(int destinationSize, bool successfulExpected, string expected)
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Skip(1)
                .Map(x => x.Name)
                .Map(x => x.Birthday, "yyyy.MM.dd")
                .Skip(1)
                .Map(x => x.Money)
                .Map(x => x.Color)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().Be(successfulExpected);

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);

            if (success)
                unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            // Arrange

            var writer = new VariableLengthWriterSequentialBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance)
                .Map(x => x.Date)
                .Map(x => x.Debit)
                .DefaultTypeConvert<decimal>((span, value) => (((long)(value * 100)).TryFormat(span, out var written), written))
                .DefaultTypeConvert<DateTime>((span, value) => (value.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Build(" ; ");

            var instance = (Balance: 0123456789.01M,
                            Date: new DateTime(2020, 05, 23),
                            Debit: 0123.45M);

            Span<char> destination = stackalloc char[33];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("12345678901 ; 23052020 ; 12345");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            // Arrange

            var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Skip(1)
                .Map(x => x.Name, SpanExtensions.ToUpperInvariant)
                .Map(x => x.Birthday, (span, date) => (date.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Map(x => x.Money)
                .Map(x => x.Nickname, (span, text) => SpanExtensions.ToUpperInvariant(span, text.Slice(0, 4)))
                .Build(" ; ");

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 123.45M,
                            Nickname: "nick name");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(" ; FOO BAR BAZ ; 23052020 ; 123.45 ; NICK");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            // Assert

            var writer = new VariableLengthWriterSequentialBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(x => x.MotherAge)
                .Map(x => x.FatherAge)
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build(" ; ");

            var instance = (Age: 15,
                            MotherAge: 40,
                            FatherAge: 50);

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("30 ; 42 ; 52");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_non_member_expression_on_mapping_should_parse()
        {
            // Assert

            var called = 0;
            var (age, motherAge, fatherAge) = (15, 40, new Func<int>(() => { called++; return 50; }));

            var writer = new VariableLengthWriterSequentialBuilder<bool>()
                .Map(_ => age, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(_ => motherAge)
                .Map(_ => fatherAge())
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build(" ; ");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(default, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("30 ; 42 ; 52");
            unwritted.Should().Be(new string(default, freeSpace));

            called.Should().Be(1);
        }

        [Fact]
        public void Parse_enum_same_way_framework()
        {
            var writer = new VariableLengthWriterSequentialBuilder<Color>()
                .Map(x => x)
                .Build(";");

            Span<char> destination = stackalloc char[50];

            // values present in enum

            Assert(Color.Black, destination);
            Assert(Color.White, destination);
            Assert(Color.Yellow, destination);
            Assert(Color.LightBlue, destination);

            // value NOT present in enum
            Assert((Color)777, destination);

            void Assert(Color value, Span<char> span)
            {
                var expected = value.ToString();

                var success = writer.TryFormat(value, span, out var charsWritten);

                success.Should().BeTrue();
                span.Slice(0, charsWritten).Should().Be(expected);
            }
        }

        [Fact]
        public void Given_empty_enum_should_parse_same_way_framework()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<EmptyEnum>()
                .Map(x => x)
                .Build(";");

            Span<char> destination = stackalloc char[50];

            var instance = (EmptyEnum)777;
            var expected = instance.ToString();

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            destination.Slice(0, charsWritten).Should().Be(expected);
        }

        [Fact]
        public void Given_nested_mapped_property_should_create_nested_instance_to_parse()
        {
            // Arrange 

            var writer = new VariableLengthWriterSequentialBuilder<Person>()
                .Map(x => x.BirthDay, "yyyy.MM.dd")
                .Map(x => x.Name)
                .Map(x => x.Mother.BirthDay, "yyyy.MM.dd")
                .Map(x => x.Mother.Name)
                .Build(" ; ");

            var instance = new Person
            {
                BirthDay = new DateTime(2020, 05, 23),
                Name = "son name",
                Mother = new Person
                {
                    BirthDay = new DateTime(1980, 01, 15),
                    Name = "mother name",
                }
            };

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("2020.05.23 ; son name ; 1980.01.15 ; mother name");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData("pt-BR")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("ru-RU")]
        [InlineData("es-ES")]
        public void Registered_primitives_types_should_have_default_converters_which_uses_current_cultureinfo(string cultureName)
        {
            // Arrange

            var instance = new AllType
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

            var writer = new VariableLengthWriterSequentialBuilder<AllType>()
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

            .Build(" ; ");

            var values = new object[]
            {
                instance.Str,
                instance.Char,

                instance.Byte,
                instance.SByte,

                instance.Double,
                instance.Float,

                instance.Int,
                instance.UInt,

                instance.Long,
                instance.ULong,

                instance.Short,
                instance.UShort,

                instance.Guid,
                instance.Date,
                instance.TimeSpan,

                instance.Bool,
                instance.Decimal,
            };

            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            var expected = string.Join(" ; ", values.Select(x => $"{x}"));

            Span<char> destination = stackalloc char[200];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }
}
