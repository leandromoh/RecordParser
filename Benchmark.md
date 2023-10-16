## Benchmark [#e140808](https://github.com/leandromoh/RecordParser/tree/e140808855d5c599e432c61e78a2b7a67bacb660)

### VariableLength Write

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                      Method | LimitRecord | parallel | ordered |       Mean |    Error |   StdDev |        Gen0 |      Gen1 |      Gen2 | Allocated |
|-------------------------------------------- |------------ |--------- |-------- |-----------:|---------:|---------:|------------:|----------:|----------:|----------:|
| Write_VariableLength_RecordParser_Extension |      500000 |    False |       ? |   498.3 ms |  9.88 ms | 23.68 ms |           - |         - |         - |   1.63 MB |
| Write_VariableLength_RecordParser_Extension |      500000 |     True |   False |   864.3 ms | 17.18 ms | 31.84 ms |           - |         - |         - |    1.7 MB |
| Write_VariableLength_RecordParser_Extension |      500000 |     True |    True |   892.6 ms | 16.88 ms | 38.45 ms |           - |         - |         - |    1.7 MB |
|           Write_VariableLength_ManualString |      500000 |        ? |       ? |   660.0 ms | 13.13 ms | 31.20 ms |  30000.0000 |         - |         - | 121.45 MB |
|           Write_VariableLength_RecordParser |      500000 |        ? |       ? |   622.5 ms | 12.32 ms | 29.99 ms |   1000.0000 |         - |         - |   5.17 MB |
|              Write_VariableLength_FlatFiles |      500000 |        ? |       ? | 1,138.8 ms | 19.50 ms | 23.95 ms | 155000.0000 |         - |         - | 618.96 MB |
|              Write_VariableLength_CSVHelper |      500000 |        ? |       ? |   946.5 ms | 18.72 ms | 27.45 ms |  73000.0000 | 7000.0000 | 7000.0000 | 523.16 MB |
|  Write_VariableLength_SoftCircuitsCsvParser |      500000 |        ? |       ? | 1,150.2 ms | 21.87 ms | 24.31 ms | 118000.0000 |         - |         - | 473.08 MB |
|                Write_VariableLength_ZString |      500000 |        ? |       ? |   568.4 ms | 14.39 ms | 41.05 ms |   1000.0000 |         - |         - |   5.15 MB |


### VariableLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                               Method | parallel | quoted |       Mean |     Error |    StdDev |     Median |        Gen0 |        Gen1 |      Gen2 |  Allocated |
|----------------------------------------------------- |--------- |------- |-----------:|----------:|----------:|-----------:|------------:|------------:|----------:|-----------:|
|            Read_VariableLength_RecordParser_Parallel |    False |  False |   630.6 ms |  12.46 ms |  13.33 ms |   629.6 ms |   8000.0000 |   5000.0000 | 2000.0000 |  123.66 MB |
|                 Read_VariableLength_RecordParser_Raw |    False |  False | 1,069.3 ms |  17.73 ms |  17.41 ms | 1,066.0 ms |  16000.0000 |   9000.0000 | 3000.0000 |   170.1 MB |
| Read_VariableLength_FullQuoted_RecordParser_Parallel |    False |   True |   880.2 ms |  16.98 ms |  18.87 ms |   874.6 ms |   8000.0000 |   5000.0000 | 2000.0000 |  123.66 MB |
|            Read_VariableLength_RecordParser_Parallel |    False |   True |   630.8 ms |  12.20 ms |  13.05 ms |   626.3 ms |   8000.0000 |   5000.0000 | 2000.0000 |  123.66 MB |
|                 Read_VariableLength_RecordParser_Raw |    False |   True | 1,097.8 ms |  19.93 ms |  49.27 ms | 1,081.0 ms |  16000.0000 |   9000.0000 | 3000.0000 |  170.09 MB |
|            Read_VariableLength_RecordParser_Parallel |     True |  False |   268.0 ms |  12.70 ms |  34.77 ms |   257.4 ms |  17500.0000 |  12500.0000 | 3000.0000 |  131.49 MB |
|                 Read_VariableLength_RecordParser_Raw |     True |  False |   779.9 ms |  62.07 ms | 175.07 ms |   692.3 ms |  24000.0000 |  14000.0000 | 4000.0000 |  292.69 MB |
| Read_VariableLength_FullQuoted_RecordParser_Parallel |     True |   True |   494.6 ms |  19.31 ms |  55.73 ms |   477.0 ms |  11000.0000 |   8000.0000 | 2000.0000 |   77.84 MB |
|            Read_VariableLength_RecordParser_Parallel |     True |   True |   275.8 ms |  16.36 ms |  46.67 ms |   259.0 ms |  18000.0000 |  14000.0000 | 3000.0000 |  139.46 MB |
|                 Read_VariableLength_RecordParser_Raw |     True |   True |   779.1 ms |  58.37 ms | 162.71 ms |   712.7 ms |  24000.0000 |  14000.0000 | 4000.0000 |  294.67 MB |
|                     Read_VariableLength_ManualString |        ? |      ? |   672.6 ms |  44.27 ms | 130.55 ms |   630.9 ms |  90000.0000 |           - |         - |  360.43 MB |
|                     Read_VariableLength_RecordParser |        ? |      ? |   536.3 ms |  30.41 ms |  87.25 ms |   494.9 ms |  12000.0000 |           - |         - |   49.38 MB |
|                        Read_VariableLength_FlatFiles |        ? |      ? | 2,526.6 ms | 215.40 ms | 631.75 ms | 2,461.8 ms | 207000.0000 |           - |         - |  825.79 MB |
|                       Read_VariableLength_ManualSpan |        ? |      ? |   478.5 ms |  34.55 ms | 100.77 ms |   418.9 ms |  12000.0000 |           - |         - |   49.32 MB |
|                        Read_VariableLength_CSVHelper |        ? |      ? | 2,254.4 ms | 202.15 ms | 592.87 ms | 1,950.0 ms |  36000.0000 |  15000.0000 | 4000.0000 |  275.33 MB |
|                    Read_VariableLength_TinyCsvParser |        ? |      ? |   839.7 ms |  68.89 ms | 196.55 ms |   752.9 ms | 282000.0000 | 120000.0000 | 2000.0000 | 1308.21 MB |
|                  Read_VariableLength_Cursively_Async |        ? |      ? |   464.7 ms |  25.70 ms |  73.75 ms |   433.6 ms |  12000.0000 |           - |         - |   49.46 MB |
|                   Read_VariableLength_Cursively_Sync |        ? |      ? |   387.8 ms |  34.30 ms | 100.60 ms |   328.4 ms |  12000.0000 |           - |         - |   49.53 MB |

### FixedLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                 Method | LimitRecord | parallel |       Mean |    Error |   StdDev |     Median |        Gen0 |      Gen1 |      Gen2 | Allocated |
|--------------------------------------- |------------ |--------- |-----------:|---------:|---------:|-----------:|------------:|----------:|----------:|----------:|
| Read_FixedLength_RecordParser_Parallel |      400000 |    False |   421.5 ms |  8.26 ms | 20.88 ms |   414.5 ms |   7000.0000 | 4000.0000 | 2000.0000 |  80.55 MB |
| Read_FixedLength_RecordParser_Parallel |      400000 |     True |   210.2 ms |  4.08 ms |  5.30 ms |   209.5 ms |  13666.6667 | 7666.6667 | 2333.3333 | 112.19 MB |
|          Read_FixedLength_ManualString |      400000 |        ? |   440.7 ms |  6.47 ms |  5.74 ms |   441.1 ms |  74000.0000 |         - |         - | 295.59 MB |
|          Read_FixedLength_RecordParser |      400000 |        ? |   295.8 ms |  5.75 ms |  6.39 ms |   296.8 ms |   9000.0000 |         - |         - |  39.51 MB |
| Read_FixedLength_RecordParser_GetLines |      400000 |        ? |   401.9 ms |  7.60 ms |  7.47 ms |   399.2 ms |   7000.0000 | 4000.0000 | 2000.0000 |  80.55 MB |
|            Read_FixedLength_ManualSpan |      400000 |        ? |   317.6 ms |  6.05 ms |  5.94 ms |   316.0 ms |   9000.0000 |         - |         - |  39.46 MB |
|             Read_FixedLength_FlatFiles |      400000 |        ? | 1,346.6 ms | 25.69 ms | 32.49 ms | 1,358.1 ms | 247000.0000 |         - |         - | 989.04 MB |

