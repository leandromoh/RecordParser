using FluentAssertions;
using RecordParser.Parsers;
using System;
using Xunit;

namespace RecordParser.Test
{
    public class CSVIndexedBuilderTest
    {
        //[Fact]
        //public void Test1()
        //{
        //    var parser = new CSVIndexedBuilder<(string Name, double Amount, DateTime ReferenceDate)>()
        //        .Map(x => x.Amount, 0)
        //        .Map(x => x.Name, 2)
        //        .Map(x => x.ReferenceDate, 4, "dd_MM_yyyy")
        //        .Build();

        //    var result = parser.Parse("11.99; foo ; identification; bar ;16_05_2020");

        //    result.Should().BeEquivalentTo((Name: "identification",
        //                                    Amount: 11.99,
        //                                    ReferenceDate: new DateTime(2020, 05, 16)));
        //}
    }
}
