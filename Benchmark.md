## Benchmark [#2e89e99](https://github.com/leandromoh/RecordParser/tree/2e89e9929dc4a0b53244466ef8fa6bae050e1e2a)

### VariableLength Write

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
|          Write_VariableLength_ManualString |      500000 |   962.2 ms | 19.13 ms | 37.31 ms |  36000.0000 |         - |         - |    146 MB |
|          Write_VariableLength_RecordParser |      500000 |   877.0 ms | 16.29 ms | 32.90 ms |   6000.0000 |         - |         - |     24 MB |
|             Write_VariableLength_FlatFiles |      500000 | 1,859.0 ms | 32.59 ms | 34.87 ms | 120000.0000 |         - |         - |    480 MB |
|             Write_VariableLength_CSVHelper |      500000 | 1,048.8 ms | 20.54 ms | 25.22 ms |  73000.0000 | 7000.0000 | 7000.0000 |    523 MB |
| Write_VariableLength_SoftCircuitsCsvParser |      500000 | 1,139.6 ms | 17.50 ms | 15.51 ms | 118000.0000 |         - |         - |    473 MB |
|               Write_VariableLength_ZString |      500000 |   792.6 ms | 15.13 ms | 16.19 ms |   6000.0000 |         - |         - |     24 MB |

### VariableLength Read

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT
  .NET 5.0 : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                              Method | LimitRecord |       Mean |    Error |    StdDev |       Gen 0 |       Gen 1 |     Gen 2 | Allocated |
|------------------------------------ |------------ |-----------:|---------:|----------:|------------:|------------:|----------:|----------:|
|    Read_VariableLength_ManualString |      500000 | 1,736.2 ms | 31.99 ms |  52.57 ms | 151000.0000 |           - |         - |    598 MB |
|    Read_VariableLength_RecordParser |      500000 |   733.6 ms | 13.67 ms |  10.67 ms |  13000.0000 |           - |         - |     53 MB |
|       Read_VariableLength_FlatFiles |      500000 | 2,835.5 ms | 66.94 ms | 185.49 ms | 183000.0000 |           - |         - |    730 MB |
|      Read_VariableLength_ManualSpan |      500000 |   964.3 ms | 19.27 ms |  27.64 ms |  17000.0000 |           - |         - |     70 MB |
|       Read_VariableLength_CSVHelper |      500000 | 2,179.3 ms | 27.66 ms |  21.60 ms | 127000.0000 |           - |         - |    504 MB |
|   Read_VariableLength_TinyCsvParser |      500000 |   985.3 ms | 14.46 ms |  12.81 ms | 279000.0000 | 113000.0000 | 2000.0000 |  1,319 MB |
| Read_VariableLength_Cursively_Async |      500000 |   559.8 ms | 10.84 ms |  17.50 ms |  16000.0000 |           - |         - |     67 MB |
|  Read_VariableLength_Cursively_Sync |      500000 |   523.6 ms | 10.38 ms |  20.00 ms |  16000.0000 |           - |         - |     66 MB |

### FixedLength Read

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT
  .NET 5.0 : .NET 5.0.1 (5.0.120.57516), X64 RyuJIT

Job=.NET 5.0  Runtime=.NET 5.0  

```
|                        Method | LimitRecord |       Mean |    Error |    StdDev |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------ |------------ |-----------:|---------:|----------:|------------:|-----------:|----------:|----------:|
| Read_FixedLength_ManualString |      400000 | 1,759.8 ms | 69.05 ms | 192.50 ms | 107000.0000 |          - |         - |    424 MB |
| Read_FixedLength_RecordParser |      400000 |   510.7 ms |  7.08 ms |   6.28 ms |  10000.0000 |          - |         - |     43 MB |
|   Read_FixedLength_ManualSpan |      400000 |   541.5 ms |  8.59 ms |   8.04 ms |  14000.0000 |          - |         - |     59 MB |
|    Read_FixedLength_FlatFiles |      400000 | 1,758.4 ms | 27.51 ms |  22.97 ms | 144000.0000 | 27000.0000 | 4000.0000 |    843 MB |


