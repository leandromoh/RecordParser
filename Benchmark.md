## Benchmark for read FixedLength [#86b03b8](https://github.com/leandromoh/RecordParser/tree/86b03b8ab871e4f49edf25d301fe4c821de768f8)


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
|      FixedLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,098.0 ms | 13.04 ms | 10.89 ms | 1,095.3 ms | 107000.0000 |     - |     - | 423.96 MB |
|    FixedLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   361.3 ms |  6.34 ms | 13.65 ms |   355.5 ms |  10000.0000 |     - |     - |  43.42 MB |
|        FixedLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   415.8 ms | 13.91 ms | 40.79 ms |   395.3 ms |  14000.0000 |     - |     - |  58.62 MB |

## Benchmark for read VariableLength [#47c4442](https://github.com/leandromoh/RecordParser/tree/47c4442425f47e9f8fa15a65c217da2b38734a0f)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1440 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.104
  [Host]        : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  .NET Core 3.1 : .NET Core 3.1.13 (CoreCLR 4.700.21.11102, CoreFX 4.700.21.11602), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


```
|                            Method |           Job |       Runtime | LimitRecord |       Mean |    Error |    StdDev |     Median |       Gen 0 |      Gen 1 |     Gen 2 |  Allocated |
|---------------------------------- |-------------- |-------------- |------------ |-----------:|---------:|----------:|-----------:|------------:|-----------:|----------:|-----------:|
|         VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,811.9 ms | 36.95 ms | 101.76 ms | 1,795.2 ms | 158000.0000 |          - |         - |  628.66 MB |
|       VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   653.8 ms | 12.24 ms |  27.13 ms |   644.2 ms |  13000.0000 |          - |         - |   54.04 MB |
|           VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   608.5 ms |  9.60 ms |   8.51 ms |   606.9 ms |  17000.0000 |          - |         - |   71.13 MB |
|     Read_VariableLength_CSVHelper | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,496.5 ms | 17.57 ms |  14.67 ms | 1,492.4 ms | 128000.0000 |          - |         - |  507.89 MB |
| Read_VariableLength_TinyCsvParser | .NET Core 3.1 | .NET Core 3.1 |      500000 |   765.3 ms | 12.77 ms |  10.67 ms |   762.7 ms | 292000.0000 | 98000.0000 | 2000.0000 | 1318.94 MB |
|         VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,445.6 ms | 10.00 ms |   8.87 ms | 1,447.0 ms | 151000.0000 |          - |         - |  597.78 MB |
|       VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   564.9 ms |  8.24 ms |   7.31 ms |   563.1 ms |  13000.0000 |          - |         - |   52.99 MB |
|           VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   565.4 ms |  9.83 ms |   8.21 ms |   564.8 ms |  17000.0000 |          - |         - |   70.08 MB |
|     Read_VariableLength_CSVHelper | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,433.8 ms | 15.01 ms |  12.53 ms | 1,428.7 ms | 127000.0000 |          - |         - |  504.03 MB |
| Read_VariableLength_TinyCsvParser | .NET Core 5.0 | .NET Core 5.0 |      500000 |   758.0 ms | 14.97 ms |  25.42 ms |   752.8 ms | 295000.0000 | 85000.0000 | 2000.0000 | 1318.94 MB |
