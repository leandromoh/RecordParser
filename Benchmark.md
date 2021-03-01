## Benchmark [#614633f](https://github.com/leandromoh/RecordParser/tree/614633ff8ddd2c4c9d1b4617a808de12002013d8)


``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1377 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.103
  [Host]        : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  .NET Core 3.1 : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                      Method |           Job |       Runtime | LimitRecord |       Mean |    Error |    StdDev |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-------------- |------------ |-----------:|---------:|----------:|------------:|------:|------:|----------:|
|      FixedLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,884.8 ms | 69.61 ms | 205.25 ms | 115000.0000 |     - |     - | 457.52 MB |
|    FixedLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   511.4 ms |  8.85 ms |   7.84 ms |  11000.0000 |     - |     - |  44.58 MB |
|        FixedLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   522.4 ms | 10.17 ms |  10.44 ms |  15000.0000 |     - |     - |  59.79 MB |
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,969.5 ms | 17.62 ms |  15.62 ms | 158000.0000 |     - |     - | 628.66 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   761.3 ms | 14.59 ms |  25.16 ms |  13000.0000 |     - |     - |  54.04 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   760.3 ms | 14.35 ms |  15.36 ms |  17000.0000 |     - |     - |  71.13 MB |
|      FixedLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,281.7 ms | 11.72 ms |   9.15 ms | 107000.0000 |     - |     - | 423.95 MB |
|    FixedLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   442.3 ms |  7.97 ms |   6.66 ms |  10000.0000 |     - |     - |  43.44 MB |
|        FixedLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   481.4 ms |  9.35 ms |  11.49 ms |  14000.0000 |     - |     - |  58.62 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,570.7 ms | 23.09 ms |  19.28 ms | 151000.0000 |     - |     - | 597.77 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   699.7 ms | 12.79 ms |  28.87 ms |  13000.0000 |     - |     - |  52.99 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   684.3 ms | 11.07 ms |  14.00 ms |  17000.0000 |     - |     - |  70.08 MB |
