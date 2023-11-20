## version 2.3.0
- support dotnet 8 (#94)

## version 2.2.1
- bugfix/first-field-quoted (#91) 
- improvement/record-too-large-exception (#90)

## version 2.1.0
- refactor/fix-namespace-extensions (#84)

## version 2.0.0
- support dotnet 7 (#52)
- support dotnet 6 (#47)
- removes legacy builds (keep only net6, net7 and netstandard2.1)
- remove obsolete methods (#76)
- support to write fixed and variable length files, using sequential or parallel processing (#66)
- support to read fixed and variable length files, using sequential or parallel processing (#51)
- skip empty row when reading file (#56)
- use new Enum.Parse overload for span (dotnet 6 optimization)
- bugfix/allow TextFindHelper get current index more than once (#46)

## version 1.3.0

- native enum methods as fallback for read/write (#42)
- support to write quoted csv record - RFC 4180 (#36)
- support to read quoted csv record - RFC 4180 (#34)
- minor improvements

## version 1.2.0

- support factory method on builders (#30)
- remove restriction for only MemberExpression on builder's Map method (#28)

## version 1.1.0

- add API documentation (#23) 
- renames Parse method to TryFormat on writers. Parse received Obsolete attribute. (#22)
- delegate clean-up
- minor improvements

## version 1.0.0

- variable-length reader/writer 
- fixed-length reader/writer 
- indexed and sequential builders
- support CultureInfo
- support .NET Core 2.1, 3.1, 5.0 and .NET Standard 2.1
