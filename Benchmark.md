## Benchmark [#afcb272](https://github.com/leandromoh/RecordParser/tree/afcb27235043578e42c07a3e8226ea1393984f38)


``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1377 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.103
  [Host]        : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT  [AttachedDebugger]
  .NET Core 3.1 : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                      Method |           Job |       Runtime | LimitRecord |        Mean |     Error |    StdDev |      Median |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-------------- |------------ |------------:|----------:|----------:|------------:|------------:|------:|------:|----------:|
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      100000 |   284.81 ms |  5.603 ms |  4.679 ms |   283.27 ms |  31000.0000 |     - |     - | 125.73 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      100000 |   132.68 ms |  3.390 ms |  9.782 ms |   130.87 ms |   3000.0000 |     - |     - |  14.29 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      100000 |   110.26 ms |  1.502 ms |  1.405 ms |   110.69 ms |   3400.0000 |     - |     - |  14.24 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |   259.12 ms |  4.475 ms | 11.062 ms |   257.49 ms |  30000.0000 |     - |     - | 119.55 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      100000 |   115.58 ms |  2.489 ms |  6.979 ms |   111.26 ms |   3400.0000 |     - |     - |  14.08 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |    98.90 ms |  0.776 ms |  0.606 ms |    98.71 ms |   3500.0000 |     - |     - |  14.03 MB |
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,525.95 ms | 12.008 ms | 10.028 ms | 1,522.65 ms | 159000.0000 |     - |     - | 628.66 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   604.47 ms | 11.022 ms | 12.693 ms |   600.44 ms |  17000.0000 |     - |     - |  71.18 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   555.77 ms |  9.013 ms | 12.927 ms |   552.08 ms |  17000.0000 |     - |     - |  71.13 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,196.19 ms | 12.758 ms | 13.651 ms | 1,192.47 ms | 151000.0000 |     - |     - | 597.78 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   537.04 ms |  9.702 ms |  8.102 ms |   535.11 ms |  17000.0000 |     - |     - |  70.13 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   500.87 ms |  9.496 ms | 13.620 ms |   497.25 ms |  17000.0000 |     - |     - |  70.08 MB |
