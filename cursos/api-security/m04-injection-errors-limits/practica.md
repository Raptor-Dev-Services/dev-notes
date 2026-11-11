# Práctica - M04 - Inyección, errores y límites

**Curso:** Seguridad de APIs: atacar y defender
**Duración:** 90 minutos, individual, en tu computadora.

> **Marco legal y ético.** Toda la práctica se hace sobre la aplicación de práctica del curso (`cursos/api-security/referencia/`), que corre en tu propia máquina con datos ficticios. No se prueba nada contra servidores de terceros, ni contra los de tu escuela o tu trabajo, sin autorización escrita. Los interruptores `Lab:*` solo existen en esta aplicación; no se copian a código real. El marco legal está en los cursos `/cybersecurity/` y `/cyber-law/`.

---

## Objetivo

1. Correr las pruebas de seguridad del módulo y leer qué comprueba cada una.
2. **Ver fallar** tres defensas quitándolas una por una: la consulta parametrizada, el manejo de errores y el tope de paginación.
3. Leer cada mensaje de falla y decir qué le cuenta a quien lo lee.
4. Escribir una prueba nueva y comprobar que mide algo.

Evidencia: la salida de cada paso, copiada a un archivo `m04-evidencia.md`.

---

## Antes de empezar

Necesitas el SDK de .NET 10 (`dotnet --version` debe empezar con `10.`), o Docker para correr el SDK en un contenedor.

```bash
cd dev-notes/cursos/api-security/referencia
dotnet --version
dotnet build
```

Si prefieres no instalar el SDK, el mismo comando corre dentro de un contenedor desechable (imagen `mcr.microsoft.com/dotnet/sdk:10.0` de Microsoft):

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test
```

El código de esta práctica **se compiló y todas sus pruebas pasaron** al escribir el módulo, con .NET 10 y xUnit 2.9.3. Los fragmentos que no pertenecen a la aplicación de práctica van marcados como "fragmento".

---

## Paso 1 - Correr las pruebas y leerlas

```bash
dotnet test --filter "Categoria=Seguridad&FullyQualifiedName~Modulo4"
```

Salida esperada:

```text
Passed!  - Failed:     0, Passed:     8, Skipped:     0, Total:     8
```

Abre `ApiSeguridad.Tests/Modulo4Tests.cs`. Para cada una de las 8 pruebas, anota en tu archivo de evidencia en **una línea**: qué comportamiento cuida y qué falla de la tabla de la clase evitaría.

| Prueba | Debe cuidar |
|---|---|
| `Un_apostrofe_en_la_busqueda_es_texto_y_no_rompe_la_consulta` | La consulta no se arma con texto |
| `Verlo_fallar_con_la_consulta_concatenada_el_apostrofe_da_error_500` | Que la prueba anterior **detecta** la falla |
| `El_cliente_no_decide_cuantos_registros_recibe` | Tope de paginación |
| `Verlo_fallar_sin_tope_la_respuesta_trae_todo_el_catalogo` | Que el tope se puede quitar y la prueba lo nota |
| `Una_busqueda_demasiado_larga_se_rechaza_con_400` | Validación de largo |
| `Un_fallo_interno_no_cuenta_como_esta_hecha_la_API` | La excepción no sale al cliente |
| `Verlo_fallar_devolver_la_excepcion_filtra_la_cadena_de_conexion` | Que la prueba del error **detecta** la fuga |
| `Un_cuerpo_enorme_se_rechaza_antes_de_leerlo` | Tope del cuerpo |

---

## Paso 2 - Ver fallar la consulta parametrizada

Abre `Modulo4Tests.cs` y, **solo en la prueba** `Un_apostrofe_en_la_busqueda_es_texto_y_no_rompe_la_consulta`, cambia la primera línea:

```csharp
using var f = new Fabrica("SqlConcatenado");   // antes: new Fabrica()
```

Ese argumento enciende el interruptor `Lab:SqlConcatenado`: la aplicación usa `Catalogo.BuscarConcatenado` en lugar de `Catalogo.Buscar`.

```bash
dotnet test --filter "FullyQualifiedName~Un_apostrofe"
```

Salida (que se obtuvo al escribir esta práctica):

```text
Failed ApiSeguridad.Tests.Modulo4Tests.Un_apostrofe_en_la_busqueda_es_texto_y_no_rompe_la_consulta
  Assert.Equal() Failure: Values differ
  Expected: OK
  Actual:   InternalServerError
```

Responde en tu evidencia:

1. ¿Qué buscaba el cliente? Un producto cuyo nombre lleva un apóstrofe. Es un dato **legítimo**.
2. ¿Qué hizo la aplicación? Pegó ese texto dentro de la sentencia; el motor no pudo leer la sentencia resultante y la API respondió 500.
3. ¿Por qué el 500 **es** el hallazgo? Un dato normal no debe provocar un error del servidor; si lo provoca, el dato está cambiando la **estructura** de lo que se ejecuta.

Abre `Catalogo.cs` y compara `Buscar` con `BuscarConcatenado`: anota **la línea exacta** donde el dato del cliente se mezcla con la sentencia.

**Restaura** `new Fabrica()` y vuelve a correr: la prueba debe pasar.

---

## Paso 3 - Ver fallar el manejo de errores

En la prueba `Un_fallo_interno_no_cuenta_como_esta_hecha_la_API`, cambia la primera línea:

```csharp
using var f = new Fabrica("ProvocarFalla", "ErroresDetallados");   // antes: new Fabrica("ProvocarFalla")
```

```bash
dotnet test --filter "FullyQualifiedName~Un_fallo_interno"
```

Salida:

```text
Failed ApiSeguridad.Tests.Modulo4Tests.Un_fallo_interno_no_cuenta_como_esta_hecha_la_API
  Assert.DoesNotContain() Failure: Sub-string found
  String: ···"se: Host=db.interna;Password=ejemplo-no-r"···
  Found:  "Password"
```

> La cadena de conexión es **ficticia** (Ejemplo ilustrativo). En una aplicación real, esa línea sería la cuenta y la contraseña de tu base.

Responde:

1. ¿Qué le cuenta esa respuesta a quien la lee? (Nombre del servidor interno, que existe una base con contraseña, el tipo de excepción.)
2. ¿Dónde **sí** debe estar ese detalle? En la bitácora del servidor, ligado al mismo `traceId` que sale en la respuesta.
3. Busca en `Infraestructura.cs` la línea de `ManejadorDeErrores` que decide si `Detail` se llena. ¿Qué la controla?

Restaura la línea y comprueba que pasa.

---

## Paso 4 - Ver fallar el tope de paginación

En la prueba `El_cliente_no_decide_cuantos_registros_recibe`:

```csharp
using var f = new Fabrica("SinTopeDePagina");   // antes: new Fabrica()
```

```bash
dotnet test --filter "FullyQualifiedName~El_cliente_no_decide"
```

Salida:

```text
Failed ApiSeguridad.Tests.Modulo4Tests.El_cliente_no_decide_cuantos_registros_recibe
  Assert.Equal() Failure: Values differ
  Expected: 50
  Actual:   120
```

Pregunta: el catálogo ficticio solo tiene 120 productos. Si tuviera 20 millones y la petición pidiera `limite=100000000`, ¿qué recursos del servidor se agotarían y cuáles de la lista de API4:2023? (Memoria, CPU, ancho de banda, la base de datos.)

Restaura y comprueba que pasa.

---

## Paso 5 - Una prueba nueva, vista fallar

Agrega al final de `Modulo4Tests` esta prueba (se compiló y pasa):

```csharp
[Fact]
public async Task Una_pagina_negativa_se_acota_a_la_primera()
{
    using var f = new Fabrica();
    using var c = f.ComoUsuario(1);
    var cuerpo = await (await c.GetAsync("/api/productos?pagina=-5")).TextAsync();
    using var doc = System.Text.Json.JsonDocument.Parse(cuerpo);
    Assert.Equal(1, doc.RootElement.GetProperty("pagina").GetInt32());
}
```

1. Córrela: debe pasar.
2. **Valídala quitando el arreglo.** En `Program.cs`, cambia `var pag = Math.Max(pagina ?? 1, 1);` por `var pag = pagina ?? 1;` y corre otra vez. ¿Qué pasó? Anota el mensaje.
3. Restaura `Math.Max` y vuelve a correrla.

Si la prueba **no** cae al quitar el arreglo, no mide nada. Esa es la regla que se aplica a cada prueba de seguridad del curso.

> Pista para el paso 2: con `pagina = -5` y `limite = 20`, el desplazamiento de la consulta es `(pagina - 1) * limite`. Observa el valor que llega a la base de datos y qué devuelve la respuesta.

---

## Paso 6 - El límite de intentos de login

El módulo 2 tiene una prueba del límite (`El_sexto_intento_de_login_en_un_minuto_se_limita`). Aquí la validas.

1. Corre solo esa prueba: debe pasar.
2. En `Program.cs`, quita `.RequireRateLimiting("login")` de la ruta de login.
3. Corre de nuevo. Anota qué cambia.
4. Restaura y corre otra vez.

Pregunta: la configuración usa **5 intentos por minuto por dirección IP**. Escribe una situación donde ese número impide el uso legítimo (varios empleados detrás de una misma dirección) y propón qué particionar en su lugar (el usuario autenticado cuando lo hay, o la combinación de usuario y dirección).

---

## Paso 7 - Los topes, medidos

Con las pruebas en verde, anota una tabla con **los topes de la aplicación** y dónde están:

| Tope | Valor | Dónde está en el código |
|---|---|---|
| Largo de la búsqueda | 50 caracteres | `Program.cs`, ruta `/productos` |
| Tamaño de página | 1 a 50, por omisión 20 | `Program.cs`, `Math.Clamp` |
| Tamaño del cuerpo | 64 KiB (65 536 bytes) | `Infraestructura.MaximoCuerpoBytes` |
| Intentos de login | 5 por minuto por IP | `Infraestructura.AgregarLimites` |

Después, completa la tabla con **otro** endpoint de la API (por ejemplo, `POST /api/pedidos/{id}/cupon`): ¿qué tope le falta? (Ninguno específico: hereda el del cuerpo; discute si necesita un límite de frecuencia propio por ser una operación que cambia dinero.)

---

## Paso 8 (opcional) - La parte de laboratorio con crAPI

Solo si tienes una máquina virtual propia con Docker. **Estos comandos no se ejecutaron al escribir el curso**; siguen el README oficial.

OWASP crAPI ("completely ridiculous API") es una API vulnerable a propósito, mantenida por OWASP (github.com/OWASP/crAPI, licencia Apache-2.0). Según su README, se instala descargando el repositorio y, en `deploy/docker`, corriendo `docker compose -f docker-compose.yml --compatibility up -d`; la aplicación web queda en `http://localhost:8888` y el correo de pruebas (Mailhog) en `http://localhost:8025`. Los retos están documentados en `docs/challenges.md` del repositorio.

Úsala **solo para reconocer** qué pasa cuando una API no limita ni valida (por ejemplo, qué responde un endpoint de crAPI cuando le mandas un dato fuera de rango) y para **escribir el control** que la defendería. La máquina virtual puede vivir en la red de laboratorio `192.168.56.0/24` de los cursos `/blue-team/` y `/pentesting-with-kali/`, si los hiciste.

---

## Qué entregas

`m04-evidencia.md` con:

1. La tabla del paso 1 (8 líneas).
2. Las tres salidas de falla (pasos 2 a 4) y tus respuestas.
3. La prueba nueva y los dos resultados (con y sin `Math.Max`).
4. La tabla de topes del paso 7.

Criterios en la tarea del módulo.

---

## Si algo no sale

| Síntoma | Causa probable |
|---|---|
| `dotnet: command not found` | No tienes el SDK; usa el contenedor del paso "Antes de empezar" |
| Las pruebas no encuentran `rutas-documentadas.txt` | Corre `dotnet test` desde la carpeta `referencia/`, no desde otra |
| Una prueba pasa cuando esperabas que fallara | Restauraste una línea a medias o corriste la prueba equivocada; revisa con `git diff` |
| Fallan pruebas de otros módulos | Dejaste un interruptor encendido en una prueba; `git checkout -- ApiSeguridad.Tests` restaura el original |
