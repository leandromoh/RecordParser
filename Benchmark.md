## Benchmark [#52dd38f](https://github.com/leandromoh/RecordParser/tree/52dd38fefc3df1e853f0bced0fee8ea320f4e13e)

### VariableLength Write

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  .NET 6.0 : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0  

```
|                                     Method | LimitRecord |       Mean |    Error |   StdDev |     Median |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------------------- |------------ |-----------:|---------:|---------:|-----------:|------------:|-----------:|----------:|----------:|
|          Write_VariableLength_ManualString |      500000 |   697.9 ms | 28.94 ms | 84.42 ms |   656.1 ms |  30000.0000 |          - |         - |    121 MB |
|          Write_VariableLength_RecordParser |      500000 |   628.4 ms | 12.34 ms | 21.29 ms |   629.3 ms |   1000.0000 |          - |         - |      5 MB |
|             Write_VariableLength_FlatFiles |      500000 | 1,130.1 ms | 22.33 ms | 27.42 ms | 1,122.5 ms | 112000.0000 |          - |         - |    447 MB |
|             Write_VariableLength_CSVHelper |      500000 |   996.0 ms | 14.98 ms | 12.51 ms |   993.9 ms |  71000.0000 | 10000.0000 | 5000.0000 |    523 MB |
| Write_VariableLength_SoftCircuitsCsvParser |      500000 | 1,086.7 ms | 14.12 ms | 11.03 ms | 1,082.0 ms | 118000.0000 |  1000.0000 |         - |    473 MB |
|               Write_VariableLength_ZString |      500000 |   560.2 ms | 10.61 ms | 10.90 ms |   558.2 ms |   1000.0000 |          - |         - |      5 MB |

### VariableLength Read

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  .NET 6.0 : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0  

```
|                              Method | LimitRecord |       Mean |    Error |   StdDev |       Gen 0 |       Gen 1 |     Gen 2 | Allocated |
|------------------------------------ |------------ |-----------:|---------:|---------:|------------:|------------:|----------:|----------:|
|    Read_VariableLength_ManualString |      500000 |   907.8 ms | 17.56 ms | 18.79 ms | 120000.0000 |           - |         - |    480 MB |
|    Read_VariableLength_RecordParser |      500000 |   664.1 ms | 13.14 ms | 28.29 ms |  12000.0000 |           - |         - |     52 MB |
|       Read_VariableLength_FlatFiles |      500000 | 1,774.2 ms | 31.76 ms | 28.15 ms | 183000.0000 |   1000.0000 |         - |    730 MB |
|      Read_VariableLength_ManualSpan |      500000 |   582.1 ms |  8.86 ms |  8.29 ms |  17000.0000 |           - |         - |     69 MB |
|       Read_VariableLength_CSVHelper |      500000 | 1,165.4 ms | 16.05 ms | 14.22 ms | 125000.0000 |           - |         - |    499 MB |
|   Read_VariableLength_TinyCsvParser |      500000 |   776.1 ms | 12.74 ms | 13.63 ms | 263000.0000 | 122000.0000 | 6000.0000 |  1,319 MB |
| Read_VariableLength_Cursively_Async |      500000 |   407.9 ms |  8.08 ms | 12.34 ms |  12000.0000 |           - |         - |     49 MB |
|  Read_VariableLength_Cursively_Sync |      500000 |   325.5 ms |  5.46 ms |  4.84 ms |  12000.0000 |           - |         - |     49 MB |

### FixedLength Read

``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1440 (1909/November2019Update/19H2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]   : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  .NET 6.0 : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=.NET 6.0  Runtime=.NET 6.0  

```
|                        Method | LimitRecord |       Mean |    Error |   StdDev |     Median |       Gen 0 |      Gen 1 |     Gen 2 | Allocated |
|------------------------------ |------------ |-----------:|---------:|---------:|-----------:|------------:|-----------:|----------:|----------:|
| Read_FixedLength_ManualString |      400000 |   693.8 ms | 22.71 ms | 65.51 ms |   672.6 ms |  74000.0000 |          - |         - |    295 MB |
| Read_FixedLength_RecordParser |      400000 |   508.8 ms |  5.37 ms |  5.02 ms |   506.9 ms |  10000.0000 |          - |         - |     42 MB |
|   Read_FixedLength_ManualSpan |      400000 |   518.1 ms |  8.06 ms |  8.96 ms |   516.0 ms |  14000.0000 |          - |         - |     57 MB |
|    Read_FixedLength_FlatFiles |      400000 | 1,555.3 ms |  9.24 ms |  7.22 ms | 1,556.4 ms | 144000.0000 | 27000.0000 | 4000.0000 |    843 MB |

