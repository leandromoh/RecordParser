using FluentAssertions;
using RecordParser.Engines.Reader;
using System;
using Xunit;

namespace RecordParser.Test
{
    public class TextFindHelperTest : TestSetup
    {
        [Fact]
        public void Given_column_mapped_more_than_once_should_works()
        {
            var id = Guid.NewGuid().ToString();
            var date = "2020.05.23";
            var color = "LightBlue";

            var record = $"{id};{date};{color}";
            var finder = new TextFindHelper(record, ";", ('"', "\""));

            // Act

            var a = finder.GetValue(0);
            var b = finder.GetValue(0);
            var c = finder.GetValue(1);
            var d = finder.GetValue(2);
            var e = finder.GetValue(2);

            // Assert

            a.Should().Be(id);
            b.Should().Be(id);
            c.Should().Be(date);
            d.Should().Be(color);
            e.Should().Be(color);
        }

        [Fact]
        public void Given_access_to_past_column_should_throw()
        {
            var id = Guid.NewGuid().ToString();
            var date = "2020.05.23";
            var color = "LightBlue";

            var record = $"{id};{date};{color}";

            string a, b, c, d;
            a = b = c = d = null;

            // Act

            var action = () =>
            {
                var finder = new TextFindHelper(record, ";", ('"', "\""));

                a = finder.GetValue(0).ToString();
                b = finder.GetValue(1).ToString();
                c = finder.GetValue(2).ToString();
                d = finder.GetValue(1).ToString();
            };

            // Assert

            action.Should().Throw<Exception>().WithMessage("can only be forward");

            a.Should().Be(id);
            b.Should().Be(date);
            c.Should().Be(color);
            d.Should().BeNull();
        }
    }
}
