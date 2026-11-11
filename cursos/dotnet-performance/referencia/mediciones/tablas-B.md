##  TaskVsValueTask
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| ConTask      | 49.03 us | 149.46 us | 8.193 us |  1.02 |    0.20 | 11.4746 |   72072 B |       1.000 |
| ConValueTask | 25.57 us |  67.07 us | 3.676 us |  0.53 |    0.10 |       - |      72 B |       0.001 |

##  ParaleloVsSecuencial
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method      | Limite | Mean         | Error        | StdDev       | Median       | Ratio | RatioSD |
|------------ |------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|
| Secuencial  | 1000   |     18.20 us |     79.85 us |     4.377 us |     15.69 us |  1.04 |    0.29 |
| Plinq       | 1000   |    250.68 us |  4,901.79 us |   268.684 us |    102.16 us | 14.26 |   13.68 |
| ParallelFor | 1000   |     39.85 us |    158.35 us |     8.680 us |     35.28 us |  2.27 |    0.60 |
|             |        |              |              |              |              |       |         |
| Secuencial  | 200000 | 17,454.42 us | 43,619.34 us | 2,390.923 us | 17,009.88 us |  1.01 |    0.17 |
| Plinq       | 200000 | 16,781.11 us | 51,496.66 us | 2,822.706 us | 16,396.76 us |  0.97 |    0.18 |
| ParallelFor | 200000 | 30,754.35 us | 95,269.86 us | 5,222.062 us | 32,631.69 us |  1.78 |    0.33 |

##  Sincronizacion
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method         | Mean     | Error    | StdDev   | Ratio | RatioSD |
|--------------- |---------:|---------:|---------:|------:|--------:|
| ConLock        | 53.78 ms | 85.01 ms | 4.660 ms |  1.01 |    0.11 |
| ConInterlocked | 12.20 ms | 22.50 ms | 1.233 ms |  0.23 |    0.03 |

##  LeerArchivo
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.33GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method                | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|------:|--------:|----------:|----------:|---------:|----------:|------------:|
| ReadAllLines          | 76.28 ms | 22.71 ms | 1.245 ms |  1.00 |    0.02 | 4750.0000 | 2750.0000 | 750.0000 |  30.37 MB |        1.00 |
| ReadLines             | 18.63 ms | 33.49 ms | 1.836 ms |  0.24 |    0.02 | 4125.0000 |   31.2500 |        - |  24.85 MB |        0.82 |
| StreamReaderBuffer64K | 10.82 ms | 25.97 ms | 1.423 ms |  0.14 |    0.02 | 4156.2500 |  250.0000 |  46.8750 |  25.03 MB |        0.82 |

##  ConsultaPorCliente
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.40GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method               | Indice | Mean       | Error       | StdDev      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|--------------------- |------- |-----------:|------------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| Dapper               | False  | 8,498.0 us |  8,498.6 us |   465.84 us |  1.00 |    0.07 |       - |      - |  20.63 KB |        1.00 |
| EfCoreAsNoTracking   | False  | 9,881.9 us | 21,014.8 us | 1,151.89 us |  1.17 |    0.13 |       - |      - |  91.42 KB |        4.43 |
| EfCoreConSeguimiento | False  | 9,174.6 us | 20,863.1 us | 1,143.58 us |  1.08 |    0.13 | 15.6250 |      - | 147.22 KB |        7.14 |
|                      |        |            |             |             |       |         |         |        |           |             |
| Dapper               | True   |   264.8 us |    327.5 us |    17.95 us |  1.00 |    0.08 |  2.9297 |      - |  20.63 KB |        1.00 |
| EfCoreAsNoTracking   | True   |   551.1 us |  3,286.5 us |   180.14 us |  2.09 |    0.60 | 13.6719 | 1.9531 |  91.99 KB |        4.46 |
| EfCoreConSeguimiento | True   | 1,527.7 us | 10,018.6 us |   549.15 us |  5.79 |    1.83 | 23.4375 | 3.9063 | 148.62 KB |        7.20 |

##  NMasUno
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.50GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method                | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| UnaConsultaPorCliente | 8.861 ms | 12.384 ms | 0.6788 ms |  1.00 |    0.09 | 93.7500 |       - |       - | 604.99 KB |        1.00 |
| UnaSolaConsulta       | 6.016 ms |  7.692 ms | 0.4216 ms |  0.68 |    0.06 | 78.1250 | 39.0625 | 39.0625 | 537.89 KB |        0.89 |

