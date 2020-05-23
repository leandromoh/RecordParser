using FluentAssertions;
using RecordParser.Parsers;
using System;
using Xunit;

namespace RecordParser.Test
{
    public class FixedLengthReaderBuilderTest
    {
        [Fact]
        public void Test1()
        {
            var parser = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
                .Map(x => x.Name, 0, 12)
                .Map(x => x.Birthday, 12, 10, "dd.MM.yyyy")
                .Map(x => x.Money, 23, 6, ".99")
                .Build();

            var result = parser.Parse("foo bar baz 23.05.2020 012345");

            result.Should().BeEquivalentTo((Name: "foo bar baz",
                                            Birthday: new DateTime(2020, 05, 23),
                                            Money: 123.45M));
        }
    }
}
