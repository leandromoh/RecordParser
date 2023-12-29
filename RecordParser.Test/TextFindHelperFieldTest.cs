using FluentAssertions;
using RecordParser.Engines;
using RecordParser.Engines.Reader;
using Xunit;

namespace RecordParser.Test
{
    public class TextFindHelperFieldTest : TestSetup
    {
        [Fact]
        public void TextFindHelper_GetField_Unordered()
        {
            // Arrage

            var record = """"
            foo bar baz , 2020.05.23 , " billy is ""the guy""", 0123.45, LightBlue
            """";

            var finder = new TextFindHelperField(record, ",", QuoteHelper.Quote, 
                stackalloc (int start, int count, bool quoted)[5], 
                stackalloc char[1024]);

            // Act

            var e = finder.GetField(4);
            var b = finder.GetField(1);
            var d = finder.GetField(3);
            var a = finder.GetField(0);
            var c = finder.GetField(2);

            // Assert

            a.ToString().Should().Be("foo bar baz ");
            b.ToString().Should().Be(" 2020.05.23 ");
            c.ToString().Should().Be(" billy is \"the guy\"");
            d.ToString().Should().Be(" 0123.45");
            e.ToString().Should().Be(" LightBlue");
        }
    }
}
