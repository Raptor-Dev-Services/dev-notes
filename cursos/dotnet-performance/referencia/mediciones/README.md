# Mediciones del curso

Salidas reales de las corridas que sostienen las cifras del curso. Entorno de todas: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8 con el trabajo `ShortRun` (3 iteraciones: rápido, con mucho error), Intel Core i5-11500, contenedor de Docker con 4 CPU, en un servidor compartido y con carga (a noviembre de 2026). **Los tiempos son ruidosos**; las asignaciones de memoria no dependen de la carga.

| Archivo | Qué contiene |
|---|---|
| `tablas-A.md` | BenchmarkDotNet, módulos 1 a 3 (`ContarPalabras`, `UnirTextos`, `ReporteCsv`, `BoxingYValores`, `Busqueda`, `LinqVsCiclo`, `ParsearLineas`, `Buffers`) |
| `tablas-B.md` | BenchmarkDotNet, módulos 4 y 5 (`TaskVsValueTask`, `ParaleloVsSecuencial`, `Sincronizacion`, `LeerArchivo`, `ConsultaPorCliente`, `NMasUno`) |
| `demos.txt`, `demos2.txt` | `Demos`: JIT, generaciones, LOH, carrera de datos, y el rechazo de BenchmarkDotNet en Debug. `demos2.txt` es la versión corregida de `gc` y `race` (en `demos.txt`, el JIT eliminó la asignación y el `n++` salió exacto) |
| `resumen-C-reporte.txt`, `api-C.log` | El endpoint `/reporte` y la primera corrida de la API de ejemplo, **sin calentamiento** (arranque en frío). Los conteos de `TIME_WAIT` de `HttpClient` en `api-C.log` son acumulados: usa `api-E.log` |
| `api-E.log` | Versión definitiva de los módulos 4 y 5: espera bloqueante frente a asíncrona, con calentamiento, dos corridas de cada una; y `HttpClient` uno por petición frente a compartido, con `TIME_WAIT` antes y después |
| `tools-D.log`, `tools-D3.log` | Módulo 6: `dotnet-counters`, `dotnet-dump` (con `dumpheap -stat` y `gcroot`) y `dotnet-trace` sobre la API de ejemplo con la fuga sembrada y con la caché acotada |
| `resumen-D-contadores.txt` | Resumen de los contadores (heap y memoria residente cada 5 s) de las dos variantes |
| `carga-*.txt` | Salida del cliente de carga en cada escenario |

Las corridas se hicieron una por vez, en un contenedor `mcr.microsoft.com/dotnet/sdk:10.0` que se borró al terminar.
