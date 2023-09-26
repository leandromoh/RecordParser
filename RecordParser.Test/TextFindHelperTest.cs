using FluentAssertions;
using RecordParser.Engines.Reader;
using Xunit;

namespace RecordParser.Test
{
    public class TextFindHelperTest
    {
        [Fact]
        public void TextFindHelper_GetField_Unordered()
        {
            // Arrage

            var record = "foo bar baz ; 2020.05.23 ; 0123.45; LightBlue";
            var finder = new TextFindHelper(record, ";", ('"', "\""));

            // Act

            var d = finder.GetField(3);
            var c = finder.GetField(2);
            var b = finder.GetField(1);
            var a = finder.GetField(0);

            // Assert

            a.ToString().Should().Be("foo bar baz ");
            b.ToString().Should().Be(" 2020.05.23 ");
            c.ToString().Should().Be(" 0123.45");
            d.ToString().Should().Be(" LightBlue");
        }
    }
}
