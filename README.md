# RecordParser

RecordParser is a expression tree based parser that helps you to write maintainable, fast and simple parsers.  
It makes easier for developers to do parsing by automating non-relevant code, allowing the developer to focus on the essentials of mapping.

1. It is fast because the non-relevant code is generated with expression tree, which once compiled is almost fast as handwriting code  
2. It is even faster because you can do parsing using the [`Span`](https://docs.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay) type, a new .NET type designed to have high-performance and reduce memory allocations
3. It is composable: developer can add custom maps that will just be embedded within the expression tree  
4. It is not intrusive: all the mapping configuration is done outside of the mapped type. It keep your POCO classes with minimised complexity and dependencies  

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
public void Given_columns_to_ignore_and_value_using_standard_format_should_parse_without_extra_configuration()
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
