## Benchmark [#03752f1](https://github.com/leandromoh/RecordParser/tree/03752f1f224f1b6e0376d789060cd8b0f0c61d3e)


``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1377 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.103
  [Host]        : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT  [AttachedDebugger]
  .NET Core 3.1 : .NET Core 3.1.12 (CoreCLR 4.700.21.6504, CoreFX 4.700.21.6905), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT


```
|                      Method |           Job |       Runtime | LimitRecord |        Mean |    Error |    StdDev |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------- |-------------- |-------------- |------------ |------------:|---------:|----------:|------------:|------:|------:|----------:|
|      **FixedLength_String_Raw** | **.NET Core 3.1** | **.NET Core 3.1** |      **100000** |   **337.03 ms** | **6.635 ms** | **12.132 ms** |  **28000.0000** |     **-** |     **-** | **114.39 MB** |
|    FixedLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      100000 |   109.23 ms | 2.428 ms |  7.004 ms |   2800.0000 |     - |     - |  11.21 MB |
|        FixedLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      100000 |   102.75 ms | 1.303 ms |  1.219 ms |   3666.6667 |     - |     - |  14.95 MB |
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      100000 |   305.58 ms | 2.740 ms |  2.429 ms |  31000.0000 |     - |     - | 125.73 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      100000 |   116.88 ms | 1.323 ms |  1.238 ms |   2600.0000 |     - |     - |  10.88 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      100000 |   110.70 ms | 1.370 ms |  1.214 ms |   3400.0000 |     - |     - |  14.24 MB |
|      FixedLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |   236.67 ms | 1.827 ms |  1.709 ms |  26666.6667 |     - |     - |    106 MB |
|    FixedLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      100000 |    93.39 ms | 1.592 ms |  1.412 ms |   2666.6667 |     - |     - |  10.92 MB |
|        FixedLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |    90.41 ms | 0.759 ms |  0.634 ms |   3666.6667 |     - |     - |  14.67 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |   242.60 ms | 1.320 ms |  1.234 ms |  30000.0000 |     - |     - | 119.55 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      100000 |   101.50 ms | 0.877 ms |  0.777 ms |   2600.0000 |     - |     - |  10.67 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      100000 |    98.98 ms | 1.259 ms |  1.116 ms |   3500.0000 |     - |     - |  14.03 MB |
|      **FixedLength_String_Raw** | **.NET Core 3.1** | **.NET Core 3.1** |      **500000** | **1,114.05 ms** | **4.066 ms** |  **3.174 ms** | **115000.0000** |     **-** |     **-** | **457.53 MB** |
|    FixedLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   363.98 ms | 3.183 ms |  2.978 ms |  11000.0000 |     - |     - |  44.58 MB |
|        FixedLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   398.65 ms | 5.575 ms |  5.215 ms |  15000.0000 |     - |     - |  59.77 MB |
|   VariableLength_String_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 | 1,531.66 ms | 9.423 ms |  8.814 ms | 159000.0000 |     - |     - | 628.66 MB |
| VariableLength_Span_Builder | .NET Core 3.1 | .NET Core 3.1 |      500000 |   553.65 ms | 4.851 ms |  4.300 ms |  13000.0000 |     - |     - |  54.04 MB |
|     VariableLength_Span_Raw | .NET Core 3.1 | .NET Core 3.1 |      500000 |   540.84 ms | 4.820 ms |  4.509 ms |  17000.0000 |     - |     - |  71.13 MB |
|      FixedLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   935.04 ms | 8.643 ms |  7.662 ms | 107000.0000 |     - |     - | 423.96 MB |
|    FixedLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   338.29 ms | 4.560 ms |  4.043 ms |  10000.0000 |     - |     - |  43.44 MB |
|        FixedLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   363.12 ms | 3.235 ms |  2.526 ms |  14000.0000 |     - |     - |  58.62 MB |
|   VariableLength_String_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 | 1,270.26 ms | 8.579 ms |  8.025 ms | 151000.0000 |     - |     - | 597.78 MB |
| VariableLength_Span_Builder | .NET Core 5.0 | .NET Core 5.0 |      500000 |   520.65 ms | 7.043 ms |  6.244 ms |  13000.0000 |     - |     - |  52.99 MB |
|     VariableLength_Span_Raw | .NET Core 5.0 | .NET Core 5.0 |      500000 |   516.31 ms | 8.386 ms |  8.973 ms |  17000.0000 |     - |     - |   70.1 MB |
