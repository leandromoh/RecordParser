## Benchmark [#52dd38f](https://github.com/leandromoh/RecordParser/tree/52dd38fefc3df1e853f0bced0fee8ea320f4e13e)

### VariableLength Write

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2364/21H2/November2021Update)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                     Method | LimitRecord |       Mean |    Error |   StdDev |        Gen0 |       Gen1 |      Gen2 | Allocated |
|------------------------------------------- |------------ |-----------:|---------:|---------:|------------:|-----------:|----------:|----------:|
|          Write_VariableLength_ManualString |      500000 |   635.6 ms | 12.71 ms | 22.92 ms |  30000.0000 |          - |         - | 121.45 MB |
|          Write_VariableLength_RecordParser |      500000 |   623.3 ms | 12.35 ms | 28.13 ms |   1000.0000 |          - |         - |   5.17 MB |
|             Write_VariableLength_FlatFiles |      500000 | 1,176.2 ms | 22.87 ms | 20.27 ms | 155000.0000 |          - |         - | 618.96 MB |
|             Write_VariableLength_CSVHelper |      500000 |   938.2 ms | 17.82 ms | 17.50 ms |  71000.0000 | 10000.0000 | 5000.0000 | 523.14 MB |
| Write_VariableLength_SoftCircuitsCsvParser |      500000 | 1,213.2 ms | 26.72 ms | 78.37 ms | 118000.0000 |          - |         - | 473.08 MB |
|               Write_VariableLength_ZString |      500000 |   608.5 ms | 12.15 ms | 33.45 ms |   1000.0000 |          - |         - |   5.15 MB |

### VariableLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3208)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                    Method | LimitRecord | parallel | quoted |       Mean |    Error |   StdDev |        Gen0 |        Gen1 |      Gen2 |  Allocated |
|------------------------------------------ |------------ |--------- |------- |-----------:|---------:|---------:|------------:|------------:|----------:|-----------:|
| Read_VariableLength_RecordParser_Parallel |      500000 |    False |  False |   671.0 ms |  6.10 ms |  5.10 ms |   7000.0000 |   4000.0000 | 1000.0000 |  107.65 MB |
|      Read_VariableLength_RecordParser_Raw |      500000 |    False |  False | 1,114.6 ms | 13.91 ms | 12.33 ms |  15000.0000 |   8000.0000 | 2000.0000 |  154.09 MB |
| Read_VariableLength_RecordParser_Parallel |      500000 |    False |   True |   671.8 ms | 10.96 ms | 10.25 ms |   7000.0000 |   4000.0000 | 1000.0000 |  107.66 MB |
|      Read_VariableLength_RecordParser_Raw |      500000 |    False |   True | 1,118.7 ms | 14.47 ms | 13.54 ms |  16000.0000 |   9000.0000 | 2000.0000 |  154.09 MB |
| Read_VariableLength_RecordParser_Parallel |      500000 |     True |  False |   243.1 ms |  4.71 ms |  9.41 ms |  10666.6667 |   6333.3333 | 1000.0000 |   54.73 MB |
|      Read_VariableLength_RecordParser_Raw |      500000 |     True |  False |   625.8 ms | 12.21 ms | 17.89 ms |  22000.0000 |  12000.0000 | 3000.0000 |  276.38 MB |
| Read_VariableLength_RecordParser_Parallel |      500000 |     True |   True |   268.1 ms |  4.46 ms |  3.72 ms |  10500.0000 |   5500.0000 | 1000.0000 |   54.87 MB |
|      Read_VariableLength_RecordParser_Raw |      500000 |     True |   True |   641.3 ms | 12.78 ms | 17.06 ms |  22000.0000 |  12000.0000 | 3000.0000 |  275.11 MB |
|          Read_VariableLength_ManualString |      500000 |        ? |      ? |   502.6 ms |  9.59 ms | 10.26 ms |  90000.0000 |           - |         - |  360.43 MB |
|          Read_VariableLength_RecordParser |      500000 |        ? |      ? |   457.1 ms |  8.83 ms |  9.45 ms |  12000.0000 |           - |         - |   49.37 MB |
|             Read_VariableLength_FlatFiles |      500000 |        ? |      ? | 1,672.7 ms | 27.02 ms | 25.28 ms | 207000.0000 |           - |         - |  825.79 MB |
|            Read_VariableLength_ManualSpan |      500000 |        ? |      ? |   386.5 ms |  7.34 ms |  6.86 ms |  12000.0000 |           - |         - |   49.32 MB |
|             Read_VariableLength_CSVHelper |      500000 |        ? |      ? | 1,690.9 ms | 12.68 ms | 11.24 ms |  36000.0000 |  15000.0000 | 4000.0000 |  275.34 MB |
|         Read_VariableLength_TinyCsvParser |      500000 |        ? |      ? |   621.1 ms | 12.18 ms | 13.04 ms | 277000.0000 | 138000.0000 | 2000.0000 | 1308.19 MB |
|       Read_VariableLength_Cursively_Async |      500000 |        ? |      ? |   381.1 ms |  7.30 ms | 15.56 ms |  12000.0000 |           - |         - |    49.4 MB |
|        Read_VariableLength_Cursively_Sync |      500000 |        ? |      ? |   332.9 ms |  6.42 ms |  8.12 ms |  12000.0000 |           - |         - |   49.47 MB |

### FixedLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2364/21H2/November2021Update)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                        Method | LimitRecord |       Mean |    Error |   StdDev |     Median |        Gen0 | Allocated |
|------------------------------ |------------ |-----------:|---------:|---------:|-----------:|------------:|----------:|
| Read_FixedLength_ManualString |      400000 |   541.4 ms | 10.80 ms | 28.08 ms |   532.8 ms |  74000.0000 | 295.59 MB |
| Read_FixedLength_RecordParser |      400000 |   335.1 ms |  8.25 ms | 23.81 ms |   331.4 ms |   9500.0000 |  39.51 MB |
|   Read_FixedLength_ManualSpan |      400000 |   352.2 ms |  6.98 ms | 16.03 ms |   347.5 ms |  13000.0000 |  54.72 MB |
|    Read_FixedLength_FlatFiles |      400000 | 1,418.9 ms | 27.83 ms | 43.33 ms | 1,419.2 ms | 247000.0000 | 989.03 MB |
