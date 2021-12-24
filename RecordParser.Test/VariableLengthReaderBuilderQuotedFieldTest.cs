using FluentAssertions;
using RecordParser.Builders.Reader;
using System;
using System.Collections.Generic;
using Xunit;

namespace RecordParser.Test
{
    public partial class VariableLengthReaderBuilderTest 
    {
        [Fact]
        public void Given_all_fields_with_quotes()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("\"1997\",\"Ford\",\"Super, luxurious truck\",\"30100.99\"");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: "Super, luxurious truck",
                                            Price: 30100.99));
        }

        [Theory]
        [InlineData(",")]
        [InlineData(" , ")]
        public void Given_all_fields_with_quotes_and_spaces_between_field_and_quote(string separator)
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(separator);

            var result = reader.Parse("  \"1997\"  ,  \"Ford\"  ,  \"Super, \"\"luxurious\"\" truck\"  ,  \"30100.99\"  ");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: "Super, \"luxurious\" truck",
                                            Price: 30100.99));
        }


        [Theory]
        [InlineData(",")]
        [InlineData(" , ")]
        public void Given_all_fields_with_quotes_and_spaces_between_field_and_quote_ignored(string separator)
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Comment, 2)
                .Build(separator);

            var result = reader.Parse("  \"1997\"  ,  \"Ford\"  ,  \"Super, \"\"luxurious\"\" truck\"  ,  \"30100.99\"  ");

            result.Should().BeEquivalentTo((Year: default(int),
                                            Model: default(string),
                                            Comment: "Super, \"luxurious\" truck",
                                            Price: default(decimal)));
        }

        [Theory]
        [InlineData(",")]
        [InlineData(" , ")]
        public void Given_all_fields_with_quotes_and_spaces_between_field_and_quote_ignored_last(string separator)
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Price, 3)
                .Build(separator);

            var result = reader.Parse("  \"1997\"  ,  \"Ford\"  ,  \"Super, \"\"luxurious\"\" truck\"  ,  \"30100.99\"  ");

            result.Should().BeEquivalentTo((Year: default(int),
                                            Model: default(string),
                                            Comment: default(string),
                                            Price: 30100.99));
        }

        [Fact]
        public void Given_some_fields_with_quotes()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,Ford,\"Super, luxurious truck\",30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: "Super, luxurious truck",
                                            Price: 30100.99));
        }

        [Fact]
        public void Given_quoted_field_with_trailing_quotes()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,Ford,\"\"\"It is fast\"\"\",30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: "\"It is fast\"",
                                            Price: 30100.99));
        }

        [Fact]
        public void Given_quoted_field_with_property_convert()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1, value => value.ToUpper())
                .Map(x => x.Comment, 2, value => value.ToUpper())
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,Ford,\"\"\"It is fast\"\"\",30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "FORD",
                                            Comment: "\"IT IS FAST\"",
                                            Price: 30100.99));
        }

        [Fact]
        public void Given_skip_quoted_field()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,Ford,\"Super, luxurious truck\",30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: (string)null,
                                            Price: 30100.99));
        }

        [Theory]
        [InlineData("1997,Ford,\"Super, luxurious truck,30100.99")]
        [InlineData("1997,Ford,\"Super, \"\"luxurious truck\"\",30100.99")]
        public void Given_fields_missing_end_quote(string line)
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            Action result = () => reader.Parse(line);

            result.Should().Throw<Exception>().WithMessage("Quoted field is missing closing quote.");
        }

        [Theory]
        [InlineData("1997,Ford,\"Super, luxurious\" truck,30100.99")]
        [InlineData("1997,\"Ford,\"Super, \"\"luxurious\"\" truck\",30100.99")]
        public void Given_extra_data_after_a_quoted_field(string line)
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            Action result = () => reader.Parse(line);

            result.Should().Throw<Exception>().WithMessage("Double quote is not escaped or there is extra data after a quoted field.");
        }

        [Fact]
        public void Given_unquoted_fields_which_contains_quotes_should_interpret_as_is()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,TV 47\", Super \"luxurious\" truck,30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "TV 47\"",
                                            Comment: "Super \"luxurious\" truck",
                                            Price: 30100.99));
        }

        [Fact]
        public void Given_embedded_quotes_escaped_with_two_double_quotes()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("1997,Ford,\"Super, \"\"luxurious\"\" truck\",30100.99");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford",
                                            Comment: "Super, \"luxurious\" truck",
                                            Price: 30100.99));
        }

        public static IEnumerable<object[]> Given_empty_fields_should_parse_source()
        {
            foreach (var model in new[] { "Ford", string.Empty })
                foreach (var comment in new[] { "\"Super, \"\"luxurious\"\" truck\"", "new car", string.Empty })
                    foreach (var owner in new[] { "Bob", string.Empty })
                        yield return new object[] { model, comment, owner };
        }

        [Theory]
        [MemberData(nameof(Given_empty_fields_should_parse_source))]
        public void Given_empty_fields_should_parse(string model, string comment, string owner)
        {
            var reader = new VariableLengthReaderBuilder<(string Model, int Year, string Comment, decimal Price, string Owner)>()
                .Map(x => x.Model, 0)
                .Map(x => x.Year, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Map(x => x.Owner, 4)
                .Build(",");

            var result = reader.Parse($"{model},1997,{comment},30100.99,{owner}");

            result.Should().BeEquivalentTo((Model: model,
                                            Year: 1997,
                                            Comment: NormalizeQuotedField(comment),
                                            Price: 30100.99,
                                            Owner: owner));
        }

        [Fact]
        public void Given_fields_with_new_line_character_interpret_as_is()
        {
            var reader = new VariableLengthReaderBuilder<(int Year, string Model, string Comment, decimal Price)>()
                .Map(x => x.Year, 0)
                .Map(x => x.Model, 1)
                .Map(x => x.Comment, 2)
                .Map(x => x.Price, 3)
                .Build(",");

            var result = reader.Parse("\"\n1997\",Ford \n Model, Super \"luxu\nrious\" truck,30100.99\n");

            result.Should().BeEquivalentTo((Year: 1997,
                                            Model: "Ford \n Model",
                                            Comment: "Super \"luxu\nrious\" truck",
                                            Price: 30100.99));
        }

        public static string NormalizeQuotedField(string text) =>
            text.Trim('"').Replace("\"\"", "\"");
    }
}
