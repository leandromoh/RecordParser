using FluentAssertions;
using RecordParser.Builders.Writer;
using System;
using Xunit;
using RecordParser.Test;
using RecordParser.Parsers;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;

namespace RecordParser.Test
{
    public partial class VariableLengthWriterBuilderTest
    {
        public const string separator = " ; ";

        public static IEnumerable<object[]> Given_text_mapped_should_write_quoted_properly_theory()
        {
            var basic = new[]
            {
                "foo bar baz",
                "FOO BAR BAZ",
                "foo \"bar\" baz",
                "\"It Is Fast\""
            };

            var specialChars = new[] { ",", "\"", "\r", "\n", separator };

            var special = specialChars.Select(x => $"foo {x} BAZ");

            var result = basic.Concat(special).ToArray();

            return result.Select(x => new object[] { x });
        }

        [Theory]
        [MemberData(nameof(Given_text_mapped_should_write_quoted_properly_theory))]
        public void Given_text_mapped_should_write_quoted_properly(string value)
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string FirstName, string LastName, string NickName, string Address, string Id)>()
                .Map(x => x.FirstName, 0, (FuncSpanTIntBool<string>)null)
                .Map(x => x.LastName, 1, (FuncSpanTIntBool)null)
                .Map(x => x.NickName, 2, StringExtensions.ToUpperInvariant)
                .Map(x => x.Address, 3, SpanExtensions.ToLowerInvariant)
                .Map(x => x.Id, 4)
                .Build(separator);

            var instance = (value, value, value, value, value);
            var quoted = value.Quote(separator);
            var expectedValues = new[] { quoted, quoted, quoted.ToUpperInvariant(), quoted.ToLowerInvariant(), quoted };
            var expected = string.Join(separator, expectedValues);

            Span<char> destination = stackalloc char[100];

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

        public static IEnumerable<object[]> Given_text_shorter_than_destination_should_not_write_theory()
        {
            var situations = new[]
            {
                // raw shorter than destination
                ("Foo", 2, "", false),
                ("F\no", 2, "", false),
                // quoted shorter than destination
                ("F\no", 4, "", false),
                // quoted same length original text
                ("Foo", 3, "Foo", true),
                ("Foo", 5, "Foo", true),
                // quoted around only
                ("F\no", 5, "\"F\no\"", true),
                ("F\no", 6, "\"F\no\"", true),
                // quoted around and inside only
                ("F\"o", 6, "\"F\"\"o\"", true),
                ("F\"o", 7, "\"F\"\"o\"", true)
            };

            foreach (MapTextType mapType in Enum.GetValues(typeof(MapTextType)))
                foreach (var (value, size, expected, sucess) in situations)
                    yield return new object[] { value, size, expected, sucess, mapType };
        }

        [Theory]
        [MemberData(nameof(Given_text_shorter_than_destination_should_not_write_theory))]
        public void Given_text_shorter_than_destination_should_not_write(string value, int size, string expected, bool expectedSuccess, MapTextType mapType)
        {
            // Arrange 

            var builder = new VariableLengthWriterBuilder<(string Text, bool _)>();

            switch (mapType)
            {
                case MapTextType.None:
                    builder.Map(x => x.Text, 0);
                    break;

                case MapTextType.StringWithoutCustom:
                    builder.Map(x => x.Text, 0, (FuncSpanTIntBool<string>)null);
                    break;

                case MapTextType.SpanWithoutCustom:
                    builder.Map(x => x.Text, 0, (FuncSpanTIntBool)null);
                    break;

                case MapTextType.StringWithCustom:
                    builder.Map(x => x.Text, 0, StringExtensions.ToUpperInvariant);
                    expected = expected.ToUpperInvariant();
                    break;

                case MapTextType.SpanWithCustom:
                    builder.Map(x => x.Text, 0, SpanExtensions.ToLowerInvariant);
                    expected = expected.ToLowerInvariant();
                    break;
            }

            var writer = builder.Build(separator);
            var instance = (value, false);

            Span<char> destination = stackalloc char[size];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().Be(expectedSuccess);

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be(expected);

            if (success)
                unwritted.Should().Be(new string(default, freeSpace));
        }

        public enum MapTextType
        {
            None,

            StringWithCustom,
            StringWithoutCustom,

            SpanWithCustom,
            SpanWithoutCustom,
        }
    }
}
