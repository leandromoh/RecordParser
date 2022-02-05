[![Nuget](https://img.shields.io/nuget/v/recordparser)](https://www.nuget.org/packages/recordparser)
![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/leandromoh/RecordParser/Validate%20dotnet/master)
![GitHub](https://img.shields.io/github/license/leandromoh/recordparser)
![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/leandromoh/RecordParser)

# RecordParser - Simple, Fast, GC friendly & Extensible

RecordParser is a expression tree based parser that helps you to write maintainable parsers with high-performance and zero allocations, thanks to Span type.
It makes easier for developers to do parsing by automating non-relevant code, which allow you to focus on the essentials of mapping.

## 🏆 3rd place in [The fastest CSV parser in .NET](https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers) blog post

Even the focus of this library being data mapping to objects (classes or structs), it got an excellent result in the blog benchmark which tested how fast libraries can transform a CSV row into an array of strings. We got 3rd place by parsing a 1 million lines file in ~1.8 seconds.

## RecordParser is a Zero Allocation Writer/Reader Parser for .NET Core

1. It supports .NET Core 2.1, 3.1, 5.0, 6.0 and .NET Standard 2.1
2. It has minimal heap allocations because it does intense use of [Span](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay) type, a new .NET type designed to have high-performance and reduce memory allocations [(see benchmark)](/Benchmark.md)
3. It is even more performant because the relevant code is generated using [expression trees](https://docs.microsoft.com/dotnet/csharp/expression-trees), which once compiled is almost fast as handwriting code
4. It supports to parse classes and structs types, without doing [boxing](https://docs.microsoft.com/dotnet/csharp/programming-guide/types/boxing-and-unboxing)
5. It is flexible: you can choose the most convenient way to configure each of your parsers: indexed or sequential configuration
6. It is extensible: you can totally customize your parsing with lambdas/delegates 
7. It is even more extensible because you can easily create extension methods that wraps custom mappings
8. It is not intrusive: all mapping configuration is done outside of the mapped type. It keeps your classes with minimised dependencies and low coupling  
9. It provides clean API with familiar methods: Parse, TryParse and TryFormat
10. It is easy configurated with a builder object, even programmatically, because does not require to define a class each time you want to define a parser
11. Compliant with [RFC 4180](https://www.ietf.org/rfc/rfc4180.txt) standard

## Benchmark

Libraries always say themselves have great perfomance, but how often they show you a benchmark comparing with other libraries? 
Check the [benchmark page](/Benchmark.md) to see RecordParser comparisons. If you miss some, a PR is welcome.

Third Party Benchmarks
- [The fastest CSV parser in .NET](https://www.joelverhagen.com/blog/2020/12/fastest-net-csv-parsers)
- [Sylvan Benchmarks](https://github.com/MarkPflug/Benchmarks)

## Currently there are parsers for 2 record formats: 
1. Fixed length, common in positional files, e.g. financial services, mainframe use, etc
    * [Reader](#fixed-length-reader)
    * [Writer](#fixed-length-writer)
3. Variable length, common in delimited files, e.g. CSV, TSV files, etc
    * [Reader](#variable-length-reader)
    * [Writer](#variable-length-writer)

## Fixed Length Reader
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

## Variable Length Reader
There are 2 flavors for mapping: indexed or sequential.  

Indexed is useful when you want to map columns by its indexes. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    var reader = new VariableLengthReaderBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
        .Map(x => x.Name, indexColumn: 0)
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
### Default Type Convert - Reader

You can define default converters for some type if you has a custom format.  
The following example defines all decimals values will be divided by 100 before assigning,  
furthermore all dates being parsed on `ddMMyyyy` format.  
This feature is avaible for both fixed and variable length.  

```csharp
[Fact]
public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
{
    var reader = new FixedLengthReaderBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
        .Map(x => x.Balance, 0, 12)
        .Map(x => x.Date, 13, 8)
        .Map(x => x.Debit, 22, 6)
        .DefaultTypeConvert(value => decimal.Parse(value) / 100)
        .DefaultTypeConvert(value => DateTime.ParseExact(value, "ddMMyyyy", null))
        .Build();

    var result = reader.Parse("012345678901 23052020 012345");

    result.Should().BeEquivalentTo((Balance: 0123456789.01M,
                                    Date: new DateTime(2020, 05, 23),
                                    Debit: 123.45M));
}
```
### Custom Property Convert - Reader

You can define a custom converter for field/property.  
Custom converters have priority case a default type convert is defined.  
This feature is avaible for both fixed and variable length.  

```csharp
[Fact]
public void Given_members_with_custom_format_should_use_custom_parser()
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
### Nested Properties Mapping - Reader

Just like a regular property, you can also configure nested properties mapping.  
The nested objects are created only if it was mapped, which avoids stack overflow problems.  
This feature is avaible for both fixed and variable length.  
 
```csharp
[Fact]
public void Given_nested_mapped_property_should_create_nested_instance_to_parse()
{
    var reader = new VariableLengthReaderBuilder<Person>()
        .Map(x => x.BirthDay, 0)
        .Map(x => x.Name, 1)
        .Map(x => x.Mother.BirthDay, 2)
        .Map(x => x.Mother.Name, 3)
        .Build(";");

    var result = reader.Parse("2020.05.23 ; son name ; 1980.01.15 ; mother name");

    result.Should().BeEquivalentTo(new Person
    {
        BirthDay = new DateTime(2020, 05, 23),
        Name = "son name",
        Mother = new Person
        {
            BirthDay = new DateTime(1980, 01, 15),
            Name = "mother name",
        }
    });
}
```

## Fixed Length Writer
There are 2 flavors for mapping: indexed or sequential.  

Both indexed and sequential builders accept the following optional parameters in `Map` methods: 
- format
- padding direction 
- padding character

Indexed is useful when you want to map columns by its position: start/length. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    // Arrange

    var writer = new FixedLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name, startIndex: 0, length: 12)
        .Map(x => x.Birthday, 12, 11, "yyyy.MM.dd", paddingChar: ' ')
        .Map(x => x.Money, 23, 7, precision: 2)
        .Build();

    var instance = (Name: "foo bar baz",
                    Birthday: new DateTime(2020, 05, 23),
                    Money: 01234.567M);

    // create buffer with 50 positions, all set to white space by default
    Span<char> destination = Enumerable.Repeat(element: ' ', count: 50).ToArray();

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("foo bar baz 2020.05.23 0123456");
}
```
Sequential is useful when you want to map columns by its order, so you just need specify the lengths.

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    // Arrange

    var writer = new FixedLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name, length: 11)
        .Skip(1)
        .Map(x => x.Birthday, 10, "yyyy.MM.dd")
        .Skip(1)
        .Map(x => x.Money, 7, precision: 2)
        .Build();

    var instance = (Name: "foo bar baz",
                    Birthday: new DateTime(2020, 05, 23),
                    Money: 01234.567M);

    // create buffer with 50 positions, all set to white space by default
    Span<char> destination = Enumerable.Repeat(element: ' ', count: 50).ToArray();

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("foo bar baz 2020.05.23 0123456");
}
```

## Variable Length Writer
There are 2 flavors for mapping: indexed or sequential.  

Both indexed and sequential builders accept the format optional parameter in `Map` method.

Indexed is useful when you want to map columns by its indexes. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    // Arrange 

    var writer = new VariableLengthWriterBuilder<(string Name, DateTime Birthday, decimal Money, Color Color)>()
        .Map(x => x.Name, indexColumn: 0)
        .Map(x => x.Birthday, 1, "yyyy.MM.dd")
        .Map(x => x.Money, 2)
        .Map(x => x.Color, 3)
        .Build(" ; ");

    var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M, Color.LightBlue);

    Span<char> destination = new char[100];

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("foo bar baz ; 2020.05.23 ; 123.45 ; LightBlue");
}
```

Sequential is useful when you want to map columns by its order. 

```csharp
[Fact]
public void Given_value_using_standard_format_should_parse_without_extra_configuration()
{
    // Arrange 

    var writer = new VariableLengthWriterSequentialBuilder<(string Name, DateTime Birthday, decimal Money)>()
        .Map(x => x.Name)
        .Skip(1)
        .Map(x => x.Birthday, "yyyy.MM.dd")
        .Map(x => x.Money)
        .Build(" ; ");

    var instance = ("foo bar baz", new DateTime(2020, 05, 23), 0123.45M);

    Span<char> destination = new char[100];

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("foo bar baz ;  ; 2020.05.23 ; 123.45");
}
```
### Default Type Convert - Writer

You can define default converters for some type if you has a custom format.  
The following example defines all decimals values will be multiplied by 100 before writing (precision 2),  
furthermore all dates being written on `ddMMyyyy` format.  
This feature is avaible for both fixed and variable length.

```csharp
[Fact]
public void Given_types_with_custom_format_should_allow_define_default_parser_for_type()
{
    // Arrange

    var writer = new FixedLengthWriterBuilder<(decimal Balance, DateTime Date, decimal Debit)>()
        .Map(x => x.Balance, 0, 12, padding: Padding.Left, paddingChar: '0')
        .Map(x => x.Date, 13, 8)
        .Map(x => x.Debit, 22, 6, padding: Padding.Left, paddingChar: '0')
        .DefaultTypeConvert<decimal>((span, value) => (((long)(value * 100)).TryFormat(span, out var written), written))
        .DefaultTypeConvert<DateTime>((span, value) => (value.TryFormat(span, out var written, "ddMMyyyy"), written))
        .Build();

    var instance = (Balance: 123456789.01M,
                    Date: new DateTime(2020, 05, 23),
                    Debit: 123.45M);

    // create buffer with 50 positions, all set to white space by default
    Span<char> destination = Enumerable.Repeat(element: ' ', count: 50).ToArray();

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("012345678901 23052020 012345");
}
```
### Custom Property Convert - Writer

You can define a custom converter for field/property.  
Custom converters have priority case a default type convert is defined.  
This feature is avaible for both fixed and variable length.  

```csharp
[Fact]
public void Given_specified_custom_parser_for_member_should_have_priority_over_custom_parser_for_type()
{
    // Assert

    var writer = new VariableLengthWriterBuilder<(int Age, int MotherAge, int FatherAge)>()
        .Map(x => x.Age, 0)
        .Map(x => x.MotherAge, 1, (span, value) => ((value + 2).TryFormat(span, out var written), written))
        .Map(x => x.FatherAge, 2)
        .Build(" ; ");

    var instance = (Age: 15,
                    MotherAge: 40,
                    FatherAge: 50);

    Span<char> destination = new char[50];

    // Act

    var success = writer.TryFormat(instance, destination, out var charsWritten);

    // Assert

    success.Should().BeTrue();

    var result = destination.Slice(0, charsWritten);

    result.Should().Be("15 ; 42 ; 50");
}
```
