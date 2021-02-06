# RecordParser

RecordParser is a expression tree based parser that helps you to write maintainable, fast and simple parsers.  
It makes easier for developers to do parsing by automating non-relevant code, allowing the developer to focus on the essentials of mapping.

1. It is fast because the non-relevant code is generated using [expression trees](https://docs.microsoft.com/dotnet/csharp/expression-trees), which once compiled is almost fast as handwriting code  
2. It is even faster because you can parse using the [Span](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay) type, a new .NET type designed to have high-performance and reduce memory allocations
3. It is extensible: developers can easily create wrapper methods with [custom maps](https://github.com/leandromoh/RecordParser/blob/master/RecordParser.Test/FixedLengthReaderBuilderTest.cs#L82)
4. It is not intrusive: all mapping configuration is done outside of the mapped type. It keeps your POCO classes with minimised dependencies and low coupling  
5. It provides simple API: reader object provides 2 familiar methods `Parse` and `TryParse`
6. It supports to parse classes and structs types (i.e., reference and value types)

Currently there are parsers for 2 record formats: 
1. Fixed length, common in positional files, e.g. mainframe use, COBOL, etc
2. Variable length, common in delimited files, e.g. CSV, TSV files, etc

## Fixed length
There are 2 flavors for mapping: indexed or sequential.  

Indexed is useful when you want to map columns by its position: start/length. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    var reader = new FixedLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name, startIndex: 0, length: 11)
        .Map(x => x.Birthday, 12, 10)
        .Map(x => x.Money, 23, 7)
        .Build();

    var result = reader.Parse("foo bar baz 2020.05.23 0123.45");

    result.Should().BeEquivalentTo((Name: "foo bar baz",
                                    Birthday: new DateTime(2020, 05, 23),
                                    Money: 123.45M));
}
```
Sequential is useful when you want to map columns by its order, so you just need specify the lengths.

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    var reader = new FixedLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name, length: 11)
        .Skip(1)
        .Map(x => x.Birthday, 10)
        .Skip(1)
        .Map(x => x.Money, 7)
        .Build();

    var result = reader.Parse("foo bar baz 2020.05.23 0123.45");

    result.Should().BeEquivalentTo((Name: "foo bar baz",
                                    Birthday: new DateTime(2020, 05, 23),
                                    Money: 123.45M));
}
```

## Variable length
There are 2 flavors for mapping: indexed or sequential.  

Indexed is useful when you want to map columns by its indexes. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
        .Map(x => x.Name, indexColum: 0)
        .Map(x => x.Birthday, 1)
        .Map(x => x.Money, 2)
        .Map(x => x.Color, 3)
        .Build(";");
  
    var result = reader.Parse("foo bar baz ; 2020.05.23 ; 0123.45; LightBlue");
  
    result.Should().BeEquivalentTo((Name: "foo bar baz",
                                    Birthday: new DateTime(2020, 05, 23),
                                    Money: 123.45M,
                                    Color: Color.LightBlue));
}
```

Sequential is useful when you want to map columns by its order. 

```csharp
[Fact]
public void Given_ignored_columns_and_value_using_standard_format_should_parse_without_extra_configuration()
{
    var reader = new VariableLengthReaderSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name)
        .Skip(1)
        .Map(x => x.Birthday)
        .Skip(2)
        .Map(x => x.Money)
        .Build(";");
  
    var result = reader.Parse("foo bar baz ; IGNORE; 2020.05.23 ; IGNORE ; IGNORE ; 0123.45");
  
    result.Should().BeEquivalentTo((Name: "foo bar baz",
                                    Birthday: new DateTime(2020, 05, 23),
                                    Money: 123.45M));
}
```
### Default Type Convert

You can define default converters for some type if you has a custom format.  
Follow example defines that all decimals values will be divided by 100 before the assign, also, all dates will be parsed in the `ddMMyyyy` format.  
This feature is avaible for both fixed and variable length.

```csharp
[Fact]
public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
{
    var reader = new FixedLengthReaderBuilder<(decimal Debit, decimal Balance, DateTime Date)>()
        .Map(x => x.Balance, 0, 12)
        .Map(x => x.Date, 13, 8)
        .Map(x => x.Debit, 22, 6)
        .DefaultTypeConvert(value => decimal.Parse(value) / 100)
        .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
        .Build();

    var result = reader.Parse("012345678901 23052020 012345");

    result.Should().BeEquivalentTo((Debit: 0123.45M,
                                    Balance: 0123456789.01M,
                                    Date: new DateTime(2020, 05, 23)));
}
```
### Custom Field/Property Convert

You can define a custom converter for field/property.  
Custom converters have priority case a default type convert is defined.  
This feature is avaible for both fixed and variable length.  

```csharp
[Fact]
public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
{
    var reader = new VariableLengthReaderBuilder<(int Age, int MotherAge, int FatherAge)>()
        .Map(x => x.Age, 0)
        .Map(x => x.MotherAge, 1, value => int.Parse(value) + 3)
        .Map(x => x.FatherAge, 2)
        .Build(";");

    var result = reader.Parse(" 15 ; 40 ; 50 ");

    result.Should().BeEquivalentTo((Age: 15,
                                    MotherAge: 43,
                                    FatherAge: 50));
}
```
