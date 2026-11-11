# Código de referencia del curso

Es el código que se midió para las cifras del curso. Corre en .NET 10 (SDK 10.0.403, runtime 10.0.14 a noviembre de 2026).

| Proyecto | Para qué | Módulo |
|---|---|---|
| `Bench/` | Los benchmarks de BenchmarkDotNet. Una clase por tema; se elige con `--filter` | 1 a 5 |
| `Demos/` | Demos de consola: JIT, generaciones del GC, LOH, carrera de datos, `HttpClient` | 1, 2, 4, 5 |
| `ApiEjemplo/` | La API de ejemplo: espera bloqueante o asíncrona, `/diag`, `/reporte` y una caché con fuga sembrada | 2, 4, 6 |
| `Carga/` | Un cliente de carga mínimo, **solo para la API de ejemplo local** | 4, 6 |

```bash
cd referencia
dotnet run -c Release --project Bench -- --filter "*ContarPalabras*"      # un benchmark
dotnet run -c Release --project Demos -- gc                               # un demo
ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run -c Release --project ApiEjemplo
dotnet run -c Release --project Carga -- "http://127.0.0.1:5080/espera-async?ms=50&n={n}" 100 15
```

- Un benchmark **siempre** se corre en `-c Release`: en Debug, BenchmarkDotNet se niega.
- Corre una prueba de carga **solo contra la API de ejemplo en tu máquina**, nunca contra un servicio real.
- Los tiempos dependen de la máquina y de la carga que tenga; las asignaciones de memoria no.
