## Benchmark [#9f9b0a1](https://github.com/leandromoh/RecordParser/tree/9f9b0a1af0f3b73ce398732b58b7b48e4f82f7fa)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1440 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.104
  [Host]        : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=.NET Core 5.0  Runtime=.NET Core 5.0  

```
|                                     Method | LimitRecord |       Mean |    Error |    StdDev |       Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------------------------------------- |------------ |-----------:|---------:|----------:|------------:|----------:|----------:|----------:|
|          Write_VariableLength_ManualString |      500000 | 1,508.3 ms | 60.43 ms | 178.18 ms |  36000.0000 |         - |         - | 146.19 MB |
|          Write_VariableLength_RecordParser |      500000 |   988.3 ms | 25.18 ms |  73.85 ms |   6000.0000 |         - |         - |  24.18 MB |
|             Write_VariableLength_CSVHelper |      500000 | 1,461.7 ms | 36.84 ms | 108.06 ms |  73000.0000 | 7000.0000 | 7000.0000 | 523.14 MB |
| Write_VariableLength_SoftCircuitsCsvParser |      500000 | 1,541.1 ms | 30.81 ms |  84.35 ms | 105000.0000 |         - |         - | 419.67 MB |
|               Write_VariableLength_ZString |      500000 | 1,178.7 ms | 47.16 ms | 137.58 ms |   6000.0000 |         - |         - |   24.1 MB |

![image](https://user-images.githubusercontent.com/11452028/122142189-53cd3000-ce25-11eb-803d-4110decb0c0d.png)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1440 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.104
  [Host]        : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=.NET Core 5.0  Runtime=.NET Core 5.0  

```
|                            Method | LimitRecord |       Mean |    Error |    StdDev |     Median |       Gen 0 |       Gen 1 |     Gen 2 |  Allocated |
|---------------------------------- |------------ |-----------:|---------:|----------:|-----------:|------------:|------------:|----------:|-----------:|
|  Read_VariableLength_ManualString |      500000 | 1,936.3 ms | 89.20 ms | 254.49 ms | 1,890.7 ms | 151000.0000 |           - |         - |  597.78 MB |
|  Read_VariableLength_RecordParser |      500000 |   722.4 ms | 12.53 ms |  16.29 ms |   721.2 ms |  13000.0000 |           - |         - |   52.99 MB |
|    Read_VariableLength_ManualSpan |      500000 |   831.2 ms | 25.66 ms |  74.43 ms |   821.6 ms |  17000.0000 |           - |         - |   70.08 MB |
|     Read_VariableLength_CSVHelper |      500000 | 1,991.2 ms | 50.43 ms | 146.31 ms | 1,945.7 ms | 127000.0000 |           - |         - |  504.03 MB |
| Read_VariableLength_TinyCsvParser |      500000 |   911.2 ms | 18.01 ms |  37.99 ms |   920.0 ms | 268000.0000 | 101000.0000 | 2000.0000 | 1319.09 MB |

![image](https://user-images.githubusercontent.com/11452028/122142524-10bf8c80-ce26-11eb-9f1e-948d02bd0292.png)

``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.1440 (1909/November2018Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.104
  [Host]        : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  .NET Core 5.0 : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

Job=.NET Core 5.0  Runtime=.NET Core 5.0  

```
|                        Method | LimitRecord |       Mean |    Error |    StdDev |     Median |       Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |------------ |-----------:|---------:|----------:|-----------:|------------:|------:|------:|----------:|
| Read_FixedLength_ManualString |      400000 | 1,502.6 ms | 40.31 ms | 115.00 ms | 1,485.7 ms | 107000.0000 |     - |     - | 423.95 MB |
| Read_FixedLength_RecordParser |      400000 |   498.2 ms |  9.70 ms |  11.92 ms |   496.7 ms |  10000.0000 |     - |     - |  43.42 MB |
|   Read_FixedLength_ManualSpan |      400000 |   538.5 ms | 14.47 ms |  40.82 ms |   525.3 ms |  14000.0000 |     - |     - |  58.62 MB |

![image](https://user-images.githubusercontent.com/11452028/122142775-7f9ce580-ce26-11eb-9dcc-db1a9d35f070.png)
