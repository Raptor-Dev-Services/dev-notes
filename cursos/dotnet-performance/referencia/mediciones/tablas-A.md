##  ContarPalabras
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method   | Palabras | Mean           | Error        | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|--------- |--------- |---------------:|-------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| ConSplit | 100      |     2,893.4 ns |   1,623.5 ns |     88.99 ns |  1.00 |    0.04 |  0.8430 |  0.0076 |       - |    5304 B |        1.00 |
| ConCiclo | 100      |       694.0 ns |     338.4 ns |     18.55 ns |  0.24 |    0.01 |       - |       - |       - |         - |        0.00 |
|          |          |                |              |              |       |         |         |         |         |           |             |
| ConSplit | 10000    | 1,156,494.0 ns | 842,878.4 ns | 46,201.01 ns |  1.00 |    0.05 | 89.8438 | 50.7813 | 25.3906 |  526039 B |        1.00 |
| ConCiclo | 10000    |   179,276.8 ns |  42,735.7 ns |  2,342.49 ns |  0.16 |    0.01 |       - |       - |       - |         - |        0.00 |

##  UnirTextos
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 0.80GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method           | N    | Mean            | Error           | StdDev        | Ratio | RatioSD | Gen0      | Gen1     | Allocated  | Alloc Ratio |
|----------------- |----- |----------------:|----------------:|--------------:|------:|--------:|----------:|---------:|-----------:|------------:|
| Concatenar       | 10   |       532.48 ns |       343.82 ns |     18.846 ns |  1.00 |    0.04 |    0.2890 |        - |     1816 B |        1.00 |
| ConStringBuilder | 10   |       281.12 ns |       273.26 ns |     14.978 ns |  0.53 |    0.03 |    0.1121 |        - |      704 B |        0.39 |
| ConJoin          | 10   |        96.04 ns |       172.94 ns |      9.479 ns |  0.18 |    0.02 |    0.0254 |        - |      160 B |        0.09 |
|                  |      |                 |                 |               |       |         |           |          |            |             |
| Concatenar       | 1000 | 1,860,980.26 ns | 1,264,131.36 ns | 69,291.306 ns | 1.001 |    0.05 | 2804.6875 | 123.0469 | 17628136 B |       1.000 |
| ConStringBuilder | 1000 |    15,357.08 ns |    12,110.73 ns |    663.830 ns | 0.008 |    0.00 |    8.1177 |   0.9003 |    50976 B |       0.003 |
| ConJoin          | 1000 |     9,016.79 ns |     9,095.61 ns |    498.561 ns | 0.005 |    0.00 |    2.8229 |        - |    17800 B |       0.001 |

##  ReporteCsv
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.24GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method           | Pedidos | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0      | Gen1     | Allocated   | Alloc Ratio |
|----------------- |-------- |------------:|------------:|----------:|------:|--------:|----------:|---------:|------------:|------------:|
| Concatenando     | 100     |    64.81 us |    59.50 us |  3.261 us |  1.00 |    0.06 |   55.3589 |   0.9155 |   339.35 KB |        1.00 |
| ConStringBuilder | 100     |    24.33 us |    20.82 us |  1.141 us |  0.38 |    0.02 |    2.4719 |   0.0610 |    15.18 KB |        0.04 |
| ConCapacidad     | 100     |    25.23 us |    15.00 us |  0.822 us |  0.39 |    0.02 |    2.3499 |   0.0610 |    14.43 KB |        0.04 |
|                  |         |             |             |           |       |         |           |          |             |             |
| Concatenando     | 1000    | 3,797.04 us | 1,009.44 us | 55.331 us |  1.00 |    0.02 | 5460.9375 | 859.3750 | 33665.39 KB |       1.000 |
| ConStringBuilder | 1000    |   270.10 us |   143.10 us |  7.844 us |  0.07 |    0.00 |   23.4375 |   4.3945 |   146.73 KB |       0.004 |
| ConCapacidad     | 1000    |   227.12 us |    20.24 us |  1.109 us |  0.06 |    0.00 |   23.4375 |   2.9297 |   145.45 KB |       0.004 |

##  BoxingYValores
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method           | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|---------:|------:|--------:|--------:|--------:|----------:|------------:|
| ListaDeObject    | 172.25 us | 152.63 us | 8.366 us |  1.00 |    0.06 | 50.7813 | 33.6914 | 312.55 KB |        1.00 |
| ListaDeInt       |  32.85 us |  43.45 us | 2.381 us |  0.19 |    0.01 |  6.3477 |  0.7629 |  39.12 KB |        0.13 |
| ArregloDeClases  | 137.03 us | 150.65 us | 8.258 us |  0.80 |    0.05 | 50.7813 | 33.6914 | 312.52 KB |        1.00 |
| ArregloDeStructs |  23.28 us |  22.87 us | 1.254 us |  0.14 |    0.01 | 12.6343 |       - |  78.15 KB |        0.25 |

##  Busqueda
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.40GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method          | N     | Mean       | Error      | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |------ |-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| ListContains    | 8     |   5.714 us |   6.229 us |  0.3414 us |  1.00 |    0.07 |         - |          NA |
| HashSetContains | 8     |   4.634 us |   4.204 us |  0.2304 us |  0.81 |    0.06 |         - |          NA |
| BinarySearch    | 8     |  12.812 us |  10.050 us |  0.5509 us |  2.25 |    0.15 |         - |          NA |
|                 |       |            |            |            |       |         |           |             |
| ListContains    | 100   |   8.406 us |   5.191 us |  0.2845 us |  1.00 |    0.04 |         - |          NA |
| HashSetContains | 100   |   4.375 us |   5.677 us |  0.3112 us |  0.52 |    0.04 |         - |          NA |
| BinarySearch    | 100   |  21.310 us |  45.136 us |  2.4741 us |  2.54 |    0.27 |         - |          NA |
|                 |       |            |            |            |       |         |           |             |
| ListContains    | 10000 | 415.853 us | 314.576 us | 17.2430 us |  1.00 |    0.05 |         - |          NA |
| HashSetContains | 10000 |   6.778 us |   4.864 us |  0.2666 us |  0.02 |    0.00 |         - |          NA |
| BinarySearch    | 10000 |  88.821 us | 103.141 us |  5.6535 us |  0.21 |    0.01 |         - |          NA |

##  LinqVsCiclo
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method         | N     | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |------ |-------------:|-------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| Linq           | 100   |    324.35 ns |    519.15 ns |    28.456 ns |  1.01 |    0.11 | 0.0165 |     104 B |        1.00 |
| LinqConCaptura | 100   |    459.02 ns |    278.63 ns |    15.273 ns |  1.42 |    0.12 | 0.0401 |     256 B |        2.46 |
| Ciclo          | 100   |     84.83 ns |     58.42 ns |     3.202 ns |  0.26 |    0.02 |      - |         - |        0.00 |
|                |       |              |              |              |       |         |        |           |             |
| Linq           | 10000 | 27,862.01 ns | 18,430.89 ns | 1,010.259 ns |  1.00 |    0.04 |      - |     104 B |        1.00 |
| LinqConCaptura | 10000 | 45,550.14 ns | 66,720.65 ns | 3,657.184 ns |  1.64 |    0.12 |      - |     256 B |        2.46 |
| Ciclo          | 10000 |  7,905.87 ns | 13,377.84 ns |   733.285 ns |  0.28 |    0.02 |      - |         - |        0.00 |

##  ParsearLineas
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 2.94GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method   | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|--------- |---------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| ConSplit | 406.7 us | 890.2 us | 48.79 us |  1.01 |    0.14 | 38.0859 |  238968 B |        1.00 |
| ConSpan  | 325.4 us | 178.2 us |  9.77 us |  0.81 |    0.08 |       - |         - |        0.00 |

##  Buffers
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat) (container)
11th Gen Intel Core i5-11500 2.70GHz (Max: 3.30GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.403
| Method          | Tamano | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------- |------- |-------------:|-------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| ArregloNuevo    | 4096   |    464.95 ns |  1,684.12 ns |    92.312 ns |  1.03 |    0.26 |  0.6561 |       - |       - |    4120 B |        1.00 |
| ArrayPoolRentar | 4096   |     37.63 ns |    103.87 ns |     5.693 ns |  0.08 |    0.02 |       - |       - |       - |         - |        0.00 |
|                 |        |              |              |              |       |         |         |         |         |           |             |
| ArregloNuevo    | 100000 | 19,179.58 ns | 37,920.75 ns | 2,078.564 ns | 1.008 |    0.14 | 31.2195 | 31.2195 | 31.2195 |  100034 B |        1.00 |
| ArrayPoolRentar | 100000 |     32.05 ns |     91.71 ns |     5.027 ns | 0.002 |    0.00 |       - |       - |       - |         - |        0.00 |

