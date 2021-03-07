﻿using FluentAssertions;
using RecordParser.BuilderWrite;
using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace RecordParser.Test
{
    public class VariableLengthWriterBuilderTest : TestSetup
    {
        [Fact]
        public void Given_value_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Color, 4)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo bar baz ; 2020.05.23 ;  ;  ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 3)
                .Map(x => x.Color, 4)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(" ; foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_skip2_first_column_using_standard_format_should_parse_without_extra_configuration()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
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

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, indexColumn: 1)
                .Map(x => x.Birthday, 2, "yyyy.MM.dd")
                .Map(x => x.Money, 4)
                .Map(x => x.Color, 5)
                .Build(" ; ");

            var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[destinationSize];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().Be(successfulExpected);

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
        {
            // Arrange

            var writer = new VariableLengthWriterBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
                .Map(x => x.Balance, 0)
                .Map(x => x.Date, 1)
                .Map(x => x.Debit, 2)
                .DefaultTypeConvert<decimal>((span, value) => (((long)(value * 100)).TryFormat(span, out var written), written))
                .DefaultTypeConvert<DateTime>((span, value) => (value.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Build(" ; ");

            var instance = (Balance: 0123456789.01M,
                            Date: new DateTime(2020, 05, 23),
                            Debit: 0123.45M);

            Span<char> destination = stackalloc char[33];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("12345678901 ; 23052020 ; 12345");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_members_with_custom_format_should_use_custom_parser()
        {
            // Arrange

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, string Nickname)>()
                .Map(x => x.Name, 1, (span, text) => (true, text.AsSpan().ToUpperInvariant(span)))
                .Map(x => x.Birthday, 2, (span, date) => (date.TryFormat(span, out var written, "ddMMyyyy"), written))
                .Map(x => x.Money, 3)
                .Map(x => x.Nickname, 4, (span, text) => (true, text.AsSpan().Slice(0, 4).ToUpperInvariant(span)))
                .Build(" ; ");

            var instance = (Name: "foo bar baz",
                            Birthday: new DateTime(2020, 05, 23),
                            Money: 123.45M,
                            Nickname: "nick name");

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(" ; FOO BAR BAZ ; 23052020 ; 123.45 ; NICK");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
        {
            // Assert

            var writer = new VariableLengthWriterBuilder<(int Age, int MotherAge, int FatherAge)>()
                .Map(x => x.Age, 0, (span, value) => ((value * 2).TryFormat(span, out var written), written))
                .Map(x => x.MotherAge, 1)
                .Map(x => x.FatherAge, 2)
                .DefaultTypeConvert<int>((span, value) => ((value + 2).TryFormat(span, out var written), written))
                .Build(" ; ");

            var instance = (Age: 15,
                            MotherAge: 40,
                            FatherAge: 50);

            Span<char> destination = stackalloc char[50];

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("30 ; 42 ; 52");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Parse_enum_same_way_framework()
        {
            var writer = new VariableLengthWriterBuilder<(Color color, int)>()
                .Map(x => x.color, 0)
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
                var instance = (value, 0);

                var success = writer.Parse(instance, span, out var charsWritten);

                success.Should().BeTrue();
                span.Slice(0, charsWritten).ToString().Should().Be(expected);
            }
        }

        [Fact]
        public void Given_empty_enum_should_parse_same_way_framework()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(EmptyEnum color, bool _)>()
                .Map(x => x.color, 0)
                .Build(";");

            Span<char> destination = stackalloc char[50];

            var instance = (color: (EmptyEnum)777, false);
            var expected = instance.color.ToString();

            // Act

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();
            destination.Slice(0, charsWritten).ToString().Should().Be(expected);
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

            var success = writer.Parse(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten).ToString();
            var unwritted = destination.Slice(charsWritten).ToString();
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }
}
