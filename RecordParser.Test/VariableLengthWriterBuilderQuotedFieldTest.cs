using FluentAssertions;
using RecordParser.Builders.Writer;
using System;
using Xunit;

namespace RecordParser.Test
{
    public partial class VariableLengthWriterBuilderTest
    {
        [Fact]
        public void Given_text_using_normal_map_without_custom_should_write_as_is()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map<string>(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("foo \"bar\" baz ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_normal_map_with_custom_should_write_as_is()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, (Span<char> span, string text) => (text.AsSpan().ToUpperInvariant(span) is var written && written == text.Length, written))
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("FOO \"BAR\" BAZ ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_quote_map_without_custom_should_write_quoted()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Money, 2)
                .Map(x => x.Color, 3)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("\"foo \"\"bar\"\" baz\" ; 2020.05.23 ; 123.45 ; LightBlue");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_quote_map_with_custom_should_write_quoted()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, string Comment, Color Color, string Owner)>()
                .Map(x => x.Name, 0, (span, text) => (text.ToUpperInvariant(span) is var written && written == text.Length, written))
                .Map(x => x.Birthday, 1, "yyyy.MM.dd")
                .Map(x => x.Comment, 2)
                .Map(x => x.Color, 3)
                .Map(x => x.Owner, 4, (span, text) => (text.ToLowerInvariant(span) is var written && written == text.Length, written))
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), "\"It Is Fast\"", Color.LightBlue, "ANA BOB");

            Span<char> destination = stackalloc char[100];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be("\"FOO \"\"BAR\"\" BAZ\" ; 2020.05.23 ; \"\"\"It Is Fast\"\"\" ; LightBlue ; ana bob");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_normal_map_with_shorter_destination_should_not_write()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map<string>(x => x.Name, 0)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[11];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().BeEmpty();
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_quote_map_without_custom_and_shorter_destination_than_unquoted_version_should_not_write()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[5];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().BeEmpty();
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_quote_map_without_custom_and_shorter_destination_than_quoted_version_should_not_write()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[15];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().BeEmpty();
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Fact]
        public void Given_text_using_quote_map_with_custom_and_shorter_destination_than_quoted_version_should_not_write()
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, (span, text) => (text.ToUpperInvariant(span) is var written && written == text.Length, Math.Max(0, written)))
                .Build(" ; ");

            var instance = ("foo \"bar\" baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[16];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeFalse();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().BeEmpty();
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData(';')]
        [InlineData(',')]
        [InlineData('\n')]
        [InlineData('\r')]
        public void Given_text_using_quote_map_without_custom_and_contains_special_char(char special)
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0)
                .Build(";");

            var instance = ($"foo {special} baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[20];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be($"\"foo {special} baz\"");
            unwritted.Should().Be(new string(default, freeSpace));
        }

        [Theory]
        [InlineData(';')]
        [InlineData(',')]
        [InlineData('\n')]
        [InlineData('\r')]
        public void Given_text_using_quote_map_with_custom_and_contains_special_char(char special)
        {
            // Arrange 

            var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
                .Map(x => x.Name, 0, (span, text) => (text.ToUpperInvariant(span) is var written && written == text.Length, Math.Max(0, written)))
                .Build(";");

            var instance = ($"foo {special} baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

            Span<char> destination = stackalloc char[20];

            // Act

            var success = writer.TryFormat(instance, destination, out var charsWritten);

            // Assert

            success.Should().BeTrue();

            var result = destination.Slice(0, charsWritten);
            var unwritted = destination.Slice(charsWritten);
            var freeSpace = destination.Length - charsWritten;

            result.Should().Be($"\"FOO {special} BAZ\"");
            unwritted.Should().Be(new string(default, freeSpace));
        }
    }
}
