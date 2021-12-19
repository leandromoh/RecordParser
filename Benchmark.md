## Benchmark [#6d5bf34](https://github.com/leandromoh/RecordParser/tree/6d5bf341002e8ae1a60f8fae3da21164506f4c08)

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT
  .NET 5.0 : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                                     Method | LimitRecord |       Mean |    Error |   StdDev |       Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------------------------------------- |------------ |-----------:|---------:|---------:|------------:|----------:|----------:|----------:|
|          Write_VariableLength_ManualString |      500000 |   982.3 ms | 19.42 ms | 53.15 ms |  36000.0000 |         - |         - |    146 MB |
|          Write_VariableLength_RecordParser |      500000 |   847.2 ms | 16.57 ms | 17.73 ms |   6000.0000 |         - |         - |     24 MB |
|             Write_VariableLength_FlatFiles |      500000 | 1,796.0 ms | 34.64 ms | 45.04 ms | 120000.0000 |         - |         - |    480 MB |
|             Write_VariableLength_CSVHelper |      500000 | 1,045.5 ms | 12.52 ms | 11.10 ms |  73000.0000 | 7000.0000 | 7000.0000 |    523 MB |
| Write_VariableLength_SoftCircuitsCsvParser |      500000 | 1,165.3 ms | 12.44 ms | 11.64 ms | 118000.0000 |         - |         - |    473 MB |
|               Write_VariableLength_ZString |      500000 |   747.2 ms | 14.51 ms | 22.16 ms |   6000.0000 |         - |         - |     24 MB |


``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT
  .NET 5.0 : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                            Method | LimitRecord |       Mean |    Error |   StdDev |       Gen 0 |       Gen 1 |     Gen 2 | Allocated |
|---------------------------------- |------------ |-----------:|---------:|---------:|------------:|------------:|----------:|----------:|
|  Read_VariableLength_ManualString |      500000 | 1,739.2 ms | 20.68 ms | 18.34 ms | 151000.0000 |           - |         - |    598 MB |
|  Read_VariableLength_RecordParser |      500000 |   684.3 ms | 13.61 ms | 12.73 ms |  13000.0000 |           - |         - |     53 MB |
|     Read_VariableLength_FlatFiles |      500000 | 2,113.8 ms | 41.28 ms | 42.39 ms | 183000.0000 |           - |         - |    730 MB |
|    Read_VariableLength_ManualSpan |      500000 |   613.2 ms |  6.83 ms |  6.39 ms |  17000.0000 |           - |         - |     70 MB |
|     Read_VariableLength_CSVHelper |      500000 | 1,423.0 ms | 11.28 ms |  9.42 ms | 127000.0000 |           - |         - |    504 MB |
| Read_VariableLength_TinyCsvParser |      500000 |   749.5 ms | 14.77 ms | 13.09 ms | 279000.0000 | 116000.0000 | 1000.0000 |  1,319 MB |

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT
  .NET 5.0 : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                        Method | LimitRecord |       Mean |    Error |   StdDev |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------ |------------ |-----------:|---------:|---------:|------------:|-----------:|----------:|----------:|
| Read_FixedLength_ManualString |      400000 | 1,345.4 ms | 26.46 ms | 64.92 ms | 107000.0000 |          - |         - |    424 MB |
| Read_FixedLength_RecordParser |      400000 |   461.7 ms |  7.80 ms | 16.11 ms |  10000.0000 |          - |         - |     43 MB |
|   Read_FixedLength_ManualSpan |      400000 |   477.4 ms |  5.10 ms |  4.52 ms |  14000.0000 |          - |         - |     59 MB |
|    Read_FixedLength_FlatFiles |      400000 | 1,704.4 ms | 23.33 ms | 21.83 ms | 144000.0000 | 27000.0000 | 4000.0000 |    843 MB |

