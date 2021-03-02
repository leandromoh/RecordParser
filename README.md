# RecordParser - simple, fast, extensible parse for records

RecordParser is a expression tree based parser that helps you to write maintainable, fast and simple parsers.  
It makes easier for developers to do parsing by automating non-relevant code, allowing the developer to focus on the essentials of mapping.

### Why another library?
I looked a lot of libraries but always encounter several of the bellow problems

1. Use strings instead of [span](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay), therefore slow and wasting computational resource
2. It does not support .NET Core, therefore not taking advantages of performance improviments and new features (e.g. span, [skiplocalsinit](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-9.0/skip-localsinit.md), etc) 
3. The code executed at runtime is not as straight as handwriting code. It does a lot of object iterations during the parse, therefore more costly
4. It has flooded API with poor examples
5. It does not support both indexed and sequential configuration. You can not choose most convenient way for each case
6. It does not offer a way to ignore columns. You are forced to map what you does not care
7. It does not support to map structs, only classes (i.e., value and reference types, respectively)
8. It supports structs, but does [boxing](https://docs.microsoft.com/dotnet/csharp/programming-guide/types/boxing-and-unboxing) (improper usage of memory)
9. It parses only files, not records. If you have a bunch individual lines, you are leave in the lurch
10. It is intrusive, configuration is made with attributes in mapped type (No POCO and low coupling)
11. It requires to define a class for each type that you want to define a parser, which is something really verbose

### RecordParser came to solve these problems

1. It is fast because the relevant code is generated using [expression trees](https://docs.microsoft.com/dotnet/csharp/expression-trees), which once compiled is almost fast as handwriting code [(sometimes faster, see benchmark)](/Benchmark.md)
2. The code generated using expression tree is basically the code you would write yourself if not using any library, simple, straight and fast
3. It is even faster because it does intense use of [Span](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay) type, a new .NET type designed to have high-performance and reduce memory allocations
4. It is extensible: developers can easily create wrapper methods with [custom maps](/RecordParser.Test/FixedLengthReaderBuilderTest.cs#L85)
5. It is flexible: choose the most convenient way to configure each of your parsers (indexed or sequential configuration)
6. It is even more flexible because you can totally customize your parsing with lambdas/delegates 
7. It is not intrusive: all mapping configuration is done outside of the mapped type. It keeps your POCO classes with minimised dependencies and low coupling  
8. It provides simple API: reader objects provides 2 familiar methods Parse and TryParse
9. It supports to parse classes and structs types (i.e., reference and value types)
10. It supports .NET Core 2.1, 3.1 and 5.0

### Currently there are parsers for 2 record formats: 
1. Fixed length, common in positional files, e.g. mainframe use, COBOL, etc
2. Variable length, common in delimited files, e.g. CSV, TSV files, etc

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
### Default Type Convert

You can define default converters for some type if you has a custom format.  
The following example defines all decimals values will be divided by 100 before assigning, furthermore all dates being parsed on `ddMMyyyy` format.  
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
### Custom Property Convert

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
### Nested Properties Mapping

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
## What about writers?

It is comming soon! [PR](https://github.com/leandromoh/RecordParser/pull/7) is working in progress

## Benchmark

Check library benchmark [here](/Benchmark.md)
