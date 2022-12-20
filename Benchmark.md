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

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2364/21H2/November2021Update)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                    Method | LimitRecord | parallel | quoted |       Mean |    Error |   StdDev |        Gen0 |        Gen1 |      Gen2 | Allocated |
|------------------------------------------ |------------ |--------- |------- |-----------:|---------:|---------:|------------:|------------:|----------:|----------:|
| Read_VariableLength_RecordParser_Parallel |     1000000 |    False |  False |   698.9 ms | 13.38 ms | 12.51 ms |   8000.0000 |   5000.0000 | 2000.0000 | 123.66 MB |
|      Read_VariableLength_RecordParser_Raw |     1000000 |    False |  False | 1,165.2 ms |  9.82 ms |  8.70 ms |  16000.0000 |   9000.0000 | 3000.0000 | 170.11 MB |
| Read_VariableLength_RecordParser_Parallel |     1000000 |    False |   True |   697.2 ms |  7.18 ms |  6.36 ms |   8000.0000 |   5000.0000 | 2000.0000 | 123.66 MB |
|      Read_VariableLength_RecordParser_Raw |     1000000 |    False |   True | 1,159.6 ms | 11.08 ms |  9.82 ms |  16000.0000 |   9000.0000 | 3000.0000 | 170.11 MB |
| Read_VariableLength_RecordParser_Parallel |     1000000 |     True |  False |   233.0 ms |  3.54 ms |  3.31 ms |  10333.3333 |   4333.3333 | 1000.0000 |  62.39 MB |
|      Read_VariableLength_RecordParser_Raw |     1000000 |     True |  False |   642.1 ms | 10.41 ms |  8.69 ms |  24000.0000 |  14000.0000 | 4000.0000 | 294.66 MB |
| Read_VariableLength_RecordParser_Parallel |     1000000 |     True |   True |   281.3 ms |  5.56 ms |  9.89 ms |  12000.0000 |   7000.0000 | 2000.0000 |  82.19 MB |
|      Read_VariableLength_RecordParser_Raw |     1000000 |     True |   True |   647.1 ms | 10.29 ms | 10.57 ms |  24000.0000 |  14000.0000 | 4000.0000 | 300.47 MB |
|          Read_VariableLength_ManualString |     1000000 |        ? |      ? |   600.3 ms | 10.78 ms | 13.24 ms | 120000.0000 |           - |         - | 479.73 MB |
|          Read_VariableLength_RecordParser |     1000000 |        ? |      ? |   449.4 ms |  5.44 ms |  4.54 ms |  12000.0000 |           - |         - |  49.38 MB |
|             Read_VariableLength_FlatFiles |     1000000 |        ? |      ? | 1,615.2 ms | 29.04 ms | 27.16 ms | 207000.0000 |           - |         - | 825.78 MB |
|            Read_VariableLength_ManualSpan |     1000000 |        ? |      ? |   386.8 ms |  6.73 ms |  9.86 ms |  12000.0000 |           - |         - |  49.32 MB |
|             Read_VariableLength_CSVHelper |     1000000 |        ? |      ? |   899.0 ms | 17.80 ms | 17.48 ms |  69000.0000 |           - |         - | 275.75 MB |
|         Read_VariableLength_TinyCsvParser |     1000000 |        ? |      ? |   613.9 ms |  8.18 ms |  7.26 ms | 278000.0000 | 138000.0000 | 2000.0000 | 1308.2 MB |
|       Read_VariableLength_Cursively_Async |     1000000 |        ? |      ? |   397.0 ms |  7.79 ms | 15.57 ms |  12000.0000 |           - |         - |  49.46 MB |
|        Read_VariableLength_Cursively_Sync |     1000000 |        ? |      ? |   307.2 ms |  6.05 ms |  5.66 ms |  12000.0000 |           - |         - |  49.32 MB |


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
