# Práctica - M5 - I/O, red y base de datos

**.NET de alto rendimiento**
**Duración:** 45 a 60 minutos, individual, en tu máquina (o en un contenedor `mcr.microsoft.com/dotnet/sdk:10.0`).

---

## Objetivo

Medir la **misma consulta** con Dapper, EF Core con `AsNoTracking` y EF Core con seguimiento, con y sin índice; y ver con tus propios ojos cuántos sockets deja un `HttpClient` por petición. Al terminar decides con datos si el problema es la herramienta o el índice.

> **Todo es local.** La base es SQLite en un archivo y las peticiones HTTP van a la API de ejemplo en tu máquina. Nunca apuntes esta práctica a un servicio de producción ni a uno ajeno.

El código es [`referencia/Bench/M5_Datos.cs`](../referencia/Bench/M5_Datos.cs) y [`referencia/Demos/Program.cs`](../referencia/Demos/Program.cs). Los tiempos de tu máquina serán otros; **los bytes asignados deben parecerse** a los de esta guía.

Entorno de las salidas de esta guía: .NET 10 (SDK 10.0.403, runtime 10.0.14), BenchmarkDotNet 0.15.8, trabajo `ShortRun`, i5-11500, contenedor con 4 CPU, servidor compartido y con carga.

---

## Antes de empezar

```bash
cd cursos/dotnet-performance/referencia
dotnet --version      # esta guía se midió con 10.0.403
```

Con contenedor (uno a la vez, y bórralo al terminar):

```bash
docker run --rm -it --cpus 4 -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 bash
```

---

## Paso 1 - La consulta, con y sin índice

La clase `ConsultaPorCliente` crea una base SQLite de 100 000 pedidos y la consulta dos veces: con el parámetro `Indice` en `false` y en `true`.

```bash
dotnet run -c Release --project Bench -- --filter "*ConsultaPorCliente*" --job short
```

Tarda varios minutos. **Salida esperada** (resumida; los tiempos varían, `Allocated` no):

```
| Method               | Indice | Mean       | Ratio | Allocated |
| Dapper               | False  | 8,498.0 us |  1.00 |  20.63 KB |
| EfCoreAsNoTracking   | False  | 9,881.9 us |  1.17 |  91.42 KB |
| EfCoreConSeguimiento | False  | 9,174.6 us |  1.08 | 147.22 KB |
| Dapper               | True   |   264.8 us |  1.00 |  20.63 KB |
| EfCoreAsNoTracking   | True   |   551.1 us |  2.09 |  91.99 KB |
| EfCoreConSeguimiento | True   | 1,527.7 us |  5.79 |  148.62 KB |
```

Anota en tu hoja: `Mean` y `Allocated` de las seis filas.

---

## Paso 2 - Verlo fallar: el índice que falta

Lee la tabla por columnas:

1. **Sin índice**, ¿cuánto cambia el tiempo entre Dapper y EF Core? En la corrida de esta guía, poco: de 8.5 a 9.9 ms, dentro del error. **La herramienta no era el problema.**
2. **Con índice**, el tiempo de Dapper bajó de 8 498.0 us a 264.8 us: unas 32 veces, sin tocar una línea de C#.
3. `Allocated` de Dapper **no cambió** entre las dos (20.63 KB): el índice cambia dónde busca la base, no cuánto materializa .NET.

Ahora quítalo tú: en `ConsultaPorCliente.Preparar`, comenta la línea que hace `CREATE INDEX` y corre el benchmark con `Indice = true`. Deberías ver que "con índice" se parece a "sin índice". Es la prueba de que el cambio venía del índice y no de otra cosa. Restaura la línea al terminar.

> Esta es la lección: **quien cambia EF Core por Dapper sin mirar el índice mide la herramienta equivocada.**

---

## Paso 3 - `AsNoTracking` y memoria

Compara las dos filas de EF Core con índice:

- `EfCoreAsNoTracking`: 91.99 KB
- `EfCoreConSeguimiento`: 148.62 KB

El seguimiento asigna más porque EF Core prepara la información para detectar cambios ([documentación](https://learn.microsoft.com/en-us/ef/core/querying/tracking)). En una consulta de solo lectura, no la usas.

Responde en tu hoja: ¿cuántas veces más asigna EF Core con `AsNoTracking` que Dapper? (91.99 / 20.63; en esta guía, unas 4.5.)

---

## Paso 4 - N+1

```bash
dotnet run -c Release --project Bench -- --filter "*NMasUno*" --job short
```

**Salida esperada:**

```
| Method                | Mean     | Allocated |
| UnaConsultaPorCliente | 8.861 ms | 604.99 KB |
| UnaSolaConsulta       | 6.016 ms | 537.89 KB |
```

La diferencia es modesta porque **SQLite no tiene red**. Anota qué esperarías con una base remota, y marca tu respuesta como **hipótesis no medida**: para medirla necesitarías agregar latencia a propósito o usar una base en otra máquina.

---

## Paso 5 - Archivos: lo que retienes

```bash
dotnet run -c Release --project Bench -- --filter "*LeerArchivo*" --job short
```

**Salida esperada:**

```
| Method                | Mean     | Gen2     | Allocated |
| ReadAllLines          | 76.28 ms | 750.0000 | 30.37 MB  |
| ReadLines             | 18.63 ms |        - | 24.85 MB  |
| StreamReaderBuffer64K | 10.82 ms |  46.8750 | 25.03 MB  |
```

Responde: ¿las tres asignan casi lo mismo? Sí (cada línea es un string). ¿Qué columna distingue a `ReadAllLines`? `Gen2`.

---

## Paso 6 - Verlo fallar: un HttpClient por petición

Necesitas dos terminales. En la primera, la API de ejemplo:

```bash
ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run -c Release --project ApiEjemplo
```

En la segunda, **un demo a la vez**, cada uno en su propio proceso:

```bash
dotnet run -c Release --project Demos -- http-compartido http://127.0.0.1:5080/ping 3000
dotnet run -c Release --project Demos -- http-nuevo http://127.0.0.1:5080/ping 3000
```

**Salida esperada** en la corrida de esta guía (Linux, loopback):

```
compartido: 3000 peticiones en 11991 ms; sockets en TIME_WAIT: 228 antes, 209 despues
uno por peticion: 3000 peticiones en 3919 ms; sockets en TIME_WAIT: 210 antes, 3157 despues
```

- Los sockets: el cliente por petición dejó unos 2 947 más; el compartido, ninguno.
- Los **tiempos salieron al revés** (el compartido tardó más). No lo tomes como resultado: fueron corridas únicas, con la máquina cargada y arranque en frío. Si en tu máquina sale distinto, **no cambia la conclusión de los sockets**.
- Cierra la API con `Ctrl+C` al terminar.

---

## Paso 7 - Decidir

Con tus datos, escribe cuatro líneas:

1. ¿El primer cambio que harías a un endpoint lento de pedidos por cliente? (índice).
2. ¿`AsNoTracking`? ¿Dónde sí y dónde no?
3. ¿Qué no puedes concluir de estos números sobre PostgreSQL por red?
4. ¿Qué cifra de esta práctica **no** usarías para sostener una decisión? (los tiempos del `HttpClient`).

---

## Limpieza

```bash
exit            # si usaste contenedor; --rm lo borra
```

Los archivos temporales (`tienda-a11-*.db`, `nmasuno-a11.db`, `lineas-a11.txt`) los borra cada benchmark en su `GlobalCleanup`.
