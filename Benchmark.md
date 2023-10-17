## Benchmark [#569d86e](https://github.com/leandromoh/RecordParser/tree/569d86eb58cc2f48887e6e6002ec43f22ad53b84)

### VariableLength Write

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                      Method | LimitRecord | parallel | ordered |       Mean |    Error |   StdDev |        Gen0 |       Gen1 |      Gen2 | Allocated |
|-------------------------------------------- |------------ |--------- |-------- |-----------:|---------:|---------:|------------:|-----------:|----------:|----------:|
| Write_VariableLength_RecordParser_Extension |      500000 |    False |       ? |   496.0 ms |  9.51 ms | 21.46 ms |           - |          - |         - |   1.62 MB |
| Write_VariableLength_RecordParser_Extension |      500000 |     True |   False |   449.8 ms |  8.62 ms | 20.48 ms |  24000.0000 | 13000.0000 | 3000.0000 | 142.28 MB |
| Write_VariableLength_RecordParser_Extension |      500000 |     True |    True |   470.0 ms |  9.35 ms | 22.05 ms |  24000.0000 | 13000.0000 | 3000.0000 | 144.05 MB |
|           Write_VariableLength_ManualString |      500000 |        ? |       ? |   660.6 ms | 12.90 ms | 22.59 ms |  30000.0000 |          - |         - | 121.45 MB |
|           Write_VariableLength_RecordParser |      500000 |        ? |       ? |   633.1 ms | 12.19 ms | 30.13 ms |   1000.0000 |          - |         - |    5.2 MB |
|              Write_VariableLength_FlatFiles |      500000 |        ? |       ? | 1,184.6 ms | 19.21 ms | 40.93 ms | 155000.0000 |          - |         - | 618.96 MB |
|              Write_VariableLength_CSVHelper |      500000 |        ? |       ? |   938.6 ms | 18.68 ms | 22.24 ms |  73000.0000 |  7000.0000 | 7000.0000 | 523.16 MB |
|  Write_VariableLength_SoftCircuitsCsvParser |      500000 |        ? |       ? | 1,168.6 ms | 22.32 ms | 24.81 ms | 118000.0000 |          - |         - | 473.08 MB |
|                Write_VariableLength_ZString |      500000 |        ? |       ? |   577.8 ms | 11.44 ms | 13.17 ms |   1000.0000 |          - |         - |   5.09 MB |

### VariableLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                               Method | parallel | quoted |       Mean |    Error |   StdDev |     Median |        Gen0 |        Gen1 |      Gen2 |  Allocated |
|----------------------------------------------------- |--------- |------- |-----------:|---------:|---------:|-----------:|------------:|------------:|----------:|-----------:|
|            Read_VariableLength_RecordParser_Parallel |    False |  False |   606.6 ms | 11.78 ms | 11.01 ms |   606.7 ms |   7000.0000 |   4000.0000 | 1000.0000 |  107.66 MB |
|                 Read_VariableLength_RecordParser_Raw |    False |  False | 1,071.6 ms | 20.27 ms | 19.91 ms | 1,072.5 ms |  15000.0000 |   8000.0000 | 2000.0000 |  154.09 MB |
| Read_VariableLength_FullQuoted_RecordParser_Parallel |    False |   True |   872.1 ms | 16.05 ms | 25.46 ms |   864.2 ms |   7000.0000 |   4000.0000 | 1000.0000 |  107.66 MB |
|            Read_VariableLength_RecordParser_Parallel |    False |   True |   605.7 ms |  8.27 ms |  7.33 ms |   607.0 ms |   7000.0000 |   4000.0000 | 1000.0000 |  107.66 MB |
|                 Read_VariableLength_RecordParser_Raw |    False |   True | 1,069.3 ms | 20.04 ms | 17.76 ms | 1,068.8 ms |  15000.0000 |   8000.0000 | 2000.0000 |  154.09 MB |
|            Read_VariableLength_RecordParser_Parallel |     True |  False |   248.3 ms |  4.95 ms | 13.31 ms |   247.2 ms |  17000.0000 |  11000.0000 | 2333.3333 |  123.04 MB |
|                 Read_VariableLength_RecordParser_Raw |     True |  False |   658.6 ms | 12.96 ms | 22.02 ms |   661.8 ms |  24000.0000 |  13000.0000 | 3000.0000 |  293.84 MB |
| Read_VariableLength_FullQuoted_RecordParser_Parallel |     True |   True |   428.9 ms |  8.54 ms | 24.78 ms |   428.2 ms |  14000.0000 |   7000.0000 | 2000.0000 |   89.26 MB |
|            Read_VariableLength_RecordParser_Parallel |     True |   True |   259.5 ms |  8.40 ms | 24.49 ms |   251.9 ms |  17000.0000 |  11500.0000 | 2500.0000 |  121.69 MB |
|                 Read_VariableLength_RecordParser_Raw |     True |   True |   667.4 ms | 13.30 ms | 24.32 ms |   663.6 ms |  23000.0000 |  13000.0000 | 3000.0000 |  288.79 MB |
|                     Read_VariableLength_ManualString |        ? |      ? |   552.1 ms | 14.23 ms | 41.52 ms |   532.1 ms |  90000.0000 |           - |         - |  360.43 MB |
|                     Read_VariableLength_RecordParser |        ? |      ? |   478.7 ms |  5.34 ms |  4.46 ms |   478.4 ms |  12000.0000 |           - |         - |   49.37 MB |
|                        Read_VariableLength_FlatFiles |        ? |      ? | 1,682.7 ms | 32.14 ms | 37.01 ms | 1,681.8 ms | 207000.0000 |           - |         - |  825.79 MB |
|                       Read_VariableLength_ManualSpan |        ? |      ? |   386.5 ms |  3.09 ms |  2.74 ms |   385.9 ms |  12000.0000 |           - |         - |   49.32 MB |
|                        Read_VariableLength_CSVHelper |        ? |      ? | 1,743.4 ms | 33.32 ms | 31.17 ms | 1,737.5 ms |  37000.0000 |  15000.0000 | 4000.0000 |  275.34 MB |
|                    Read_VariableLength_TinyCsvParser |        ? |      ? |   643.4 ms |  8.22 ms |  6.87 ms |   643.7 ms | 285000.0000 | 128000.0000 | 1000.0000 | 1308.14 MB |
|                  Read_VariableLength_Cursively_Async |        ? |      ? |   417.5 ms |  9.95 ms | 29.34 ms |   415.3 ms |  12000.0000 |           - |         - |    49.4 MB |
|                   Read_VariableLength_Cursively_Sync |        ? |      ? |   316.3 ms |  5.81 ms |  5.15 ms |   317.0 ms |  12000.0000 |           - |         - |   49.47 MB |


### FixedLength Read

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.3448)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]   : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  .NET 7.0 : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

Job=.NET 7.0  Runtime=.NET 7.0  

```
|                                 Method | LimitRecord | parallel |       Mean |    Error |   StdDev |        Gen0 |      Gen1 |      Gen2 | Allocated |
|--------------------------------------- |------------ |--------- |-----------:|---------:|---------:|------------:|----------:|----------:|----------:|
| Read_FixedLength_RecordParser_Parallel |      400000 |    False |   397.2 ms |  6.23 ms |  5.52 ms |   6000.0000 | 3000.0000 | 1000.0000 |  64.55 MB |
| Read_FixedLength_RecordParser_Parallel |      400000 |     True |   206.2 ms |  4.09 ms |  9.39 ms |  13333.3333 | 6333.3333 | 2000.0000 | 105.77 MB |
|          Read_FixedLength_ManualString |      400000 |        ? |   465.4 ms |  7.51 ms |  7.02 ms |  74000.0000 |         - |         - | 295.59 MB |
|          Read_FixedLength_RecordParser |      400000 |        ? |   303.3 ms |  5.85 ms |  8.01 ms |   9500.0000 |         - |         - |  39.51 MB |
| Read_FixedLength_RecordParser_GetLines |      400000 |        ? |   406.6 ms |  7.92 ms | 10.85 ms |   6000.0000 | 3000.0000 | 1000.0000 |  64.55 MB |
|            Read_FixedLength_ManualSpan |      400000 |        ? |   332.1 ms |  6.49 ms |  5.76 ms |   9000.0000 |         - |         - |  39.45 MB |
|             Read_FixedLength_FlatFiles |      400000 |        ? | 1,355.6 ms | 26.99 ms | 37.84 ms | 247000.0000 |         - |         - | 989.04 MB |


