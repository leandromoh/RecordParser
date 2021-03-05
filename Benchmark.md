## Benchmark [#86b03b8](https://github.com/leandromoh/RecordParser/tree/86b03b8ab871e4f49edf25d301fe4c821de768f8)


``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1379 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.103
  [Host]        : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT
  .NET Core 3.1 : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                      Method |           Job |       Runtime | LimitRecord |       Mean |    Error |   StdDev |     Median |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-------------- |------------ |-----------:|---------:|---------:|-----------:|------------:|------:|------:|----------:|
|      FixedLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,401.4 ms | 30.81 ms | 89.39 ms | 1,374.0 ms | 115000.0000 |     - |     - | 457.53 MB |
|    FixedLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   432.7 ms |  8.63 ms | 20.68 ms |   423.6 ms |  11000.0000 |     - |     - |  44.56 MB |
|        FixedLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   439.7 ms |  4.06 ms |  4.51 ms |   438.7 ms |  15000.0000 |     - |     - |  59.77 MB |
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,609.6 ms | 26.17 ms | 21.85 ms | 1,600.5 ms | 158000.0000 |     - |     - | 628.66 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   604.8 ms |  6.99 ms |  6.20 ms |   604.1 ms |  13000.0000 |     - |     - |  54.04 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   594.2 ms |  8.80 ms | 14.95 ms |   587.9 ms |  17000.0000 |     - |     - |  71.13 MB |
|      FixedLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,098.0 ms | 13.04 ms | 10.89 ms | 1,095.3 ms | 107000.0000 |     - |     - | 423.96 MB |
|    FixedLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   361.3 ms |  6.34 ms | 13.65 ms |   355.5 ms |  10000.0000 |     - |     - |  43.42 MB |
|        FixedLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   415.8 ms | 13.91 ms | 40.79 ms |   395.3 ms |  14000.0000 |     - |     - |  58.62 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,409.3 ms | 18.68 ms | 19.99 ms | 1,409.3 ms | 151000.0000 |     - |     - |  597.8 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   565.6 ms | 12.70 ms | 35.20 ms |   548.6 ms |  13000.0000 |     - |     - |  52.99 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   539.2 ms |  2.65 ms |  3.80 ms |   538.0 ms |  17000.0000 |     - |     - |  70.08 MB |
