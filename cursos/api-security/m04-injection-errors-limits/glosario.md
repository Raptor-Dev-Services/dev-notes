# Glosario - M04 - Inyección, errores y límites

> Definiciones de los términos técnicos del módulo 4. Ordenado alfabéticamente.

---

## A

### API4:2023 Unrestricted Resource Consumption
**Categoría:** OWASP API Security
**Definición:** Categoría de la lista OWASP API Security Top 10 2023: la API atiende peticiones sin límite y consume CPU, memoria, ancho de banda, almacenamiento o servicios de pago de terceros.
**En el curso:** Parte 4; se cierra con topes de paginación, de cuerpo y de frecuencia.

### Apóstrofe (prueba del)
**Categoría:** Pruebas
**Definición:** Prueba de detección que manda un dato legítimo con un apóstrofe (`O'Brien`) y espera que la API lo trate como texto. Si responde con error, la consulta se arma con texto.
**En el curso:** Parte 2; `Un_apostrofe_en_la_busqueda_es_texto_y_no_rompe_la_consulta`.

---

## B

### Bitácora
**Categoría:** Operación
**Definición:** Registro del servidor donde va el detalle completo de un error. El cliente solo recibe un identificador para pedir ayuda.
**En el curso:** Parte 3; `LogError` en `ManejadorDeErrores`.

---

## C

### Concurrencia (limitador de)
**Categoría:** Límites
**Definición:** Limitador que cuenta las peticiones simultáneas, no las de un periodo. Útil en endpoints caros.
**En el curso:** Parte 4.

### Consulta parametrizada
**Categoría:** Defensa
**Definición:** Consulta cuya sentencia es fija y cuyos datos viajan aparte como parámetros; la base de datos nunca interpreta el dato como instrucción.
**En el curso:** Parte 2; `Catalogo.Buscar`.

### Content-Length
**Categoría:** HTTP
**Definición:** Cabecera con el tamaño del cuerpo en bytes. La aplicación de práctica la compara con su tope antes de leer el cuerpo.
**En el curso:** Parte 4; `UseTopeDeCuerpo`.

### Cubeta de tokens (token bucket)
**Categoría:** Límites
**Definición:** Limitador que repone tokens a un ritmo fijo y gasta uno por petición; permite ráfagas controladas.
**En el curso:** Parte 4; uno de los cuatro algoritmos de ASP.NET Core.

---

## D

### Detail
**Categoría:** RFC 9457
**Definición:** Miembro de un problema HTTP con la explicación de esta ocurrencia. No debe llevar los internos de la implementación.
**En el curso:** Parte 3.

---

## E

### Enumeración
**Categoría:** Ataque conceptual
**Definición:** Deducir qué identificadores o cuentas existen por diferencias entre respuestas ("no existe" frente a "no es tuyo").
**En el curso:** Parte 3; por eso la API responde igual en los dos casos.

### Excepción no controlada
**Categoría:** Errores
**Definición:** Excepción que nadie atrapó en el código de la aplicación y llega al manejador global.
**En el curso:** Parte 3.

---

## F

### Fixed window (ventana fija)
**Categoría:** Límites
**Definición:** Limitador de N peticiones por ventana de tiempo; al terminar la ventana el contador se reinicia.
**En el curso:** Parte 4; login con 5 por minuto.

### FromSqlRaw
**Categoría:** EF Core
**Definición:** Método de EF Core que ejecuta SQL ya armado. Según la documentación, es vulnerable si se interpola texto del cliente; `FromSql` y `FromSqlInterpolated` sí parametrizan.
**En el curso:** Parte 2.

---

## H

### HTTP 400
**Categoría:** HTTP
**Definición:** El cliente mandó un dato inválido (tipo, largo, formato).
**En el curso:** Búsqueda de más de 50 caracteres.

### HTTP 413
**Categoría:** HTTP
**Definición:** El cuerpo de la petición excede lo que el servidor acepta.
**En el curso:** `UseTopeDeCuerpo` responde con este código.

### HTTP 422
**Categoría:** HTTP
**Definición:** El dato está bien formado pero no es admisible para el negocio.
**En el curso:** Cupón desconocido; destino no permitido (M5).

### HTTP 429
**Categoría:** HTTP
**Definición:** Código definido en el RFC 6585 (2012), sección 4: el usuario envió demasiadas peticiones en un periodo.
**En el curso:** Parte 4.

---

## I

### IExceptionHandler
**Categoría:** ASP.NET Core
**Definición:** Interfaz para escribir un manejador de excepciones que se registra con `AddExceptionHandler` y se activa con `UseExceptionHandler`.
**En el curso:** `ManejadorDeErrores`.

### Interruptor Lab
**Categoría:** Práctica
**Definición:** Opción de `LabOpciones` que reintroduce una falla en la aplicación de práctica para verla fallar. Arrancan apagados.
**En el curso:** Parte 5; no existe en código real.

### Inyección
**Categoría:** Clase de falla
**Definición:** Un dato escrito por quien llama es interpretado como instrucción por otro programa (base de datos, sistema operativo, directorio, plantilla).
**En el curso:** Categoría A05 de la lista OWASP Top 10:2025.

### Inyección NoSQL
**Categoría:** Clase de falla
**Definición:** Inyección sobre una base documental: el dato cambia el filtro o el operador de la consulta.
**En el curso:** Parte 2, a nivel de idea.

---

## K

### Kestrel
**Categoría:** ASP.NET Core
**Definición:** Servidor web de ASP.NET Core. Sus límites (`KestrelServerLimits`) incluyen el tamaño máximo del cuerpo.
**En el curso:** Parte 4.

---

## L

### Lista de permitidos (allowlist)
**Categoría:** Defensa
**Definición:** Validación que acepta solo una forma conocida y rechaza lo demás. Contraria a la lista de prohibidos.
**En el curso:** Parte 1.

### Lista de prohibidos (denylist)
**Categoría:** Defensa
**Definición:** Validación que rechaza elementos conocidos como peligrosos; siempre incompleta.
**En el curso:** Parte 1.

### Límite de frecuencia (rate limiting)
**Categoría:** Límites
**Definición:** Control que restringe cuántas peticiones puede hacer un cliente en un periodo.
**En el curso:** Parte 4.

---

## M

### MaxRequestBodySize
**Categoría:** Kestrel
**Definición:** Propiedad de `KestrelServerLimits` con el tamaño máximo del cuerpo en bytes; la documentación indica 30 000 000 por omisión.
**En el curso:** Parte 4.

---

## P

### Paginación con tope
**Categoría:** Defensa
**Definición:** El servidor decide el máximo de registros por respuesta; el cliente solo sugiere.
**En el curso:** `Math.Clamp(limite ?? 20, 1, 50)`.

### Partición
**Categoría:** Límites
**Definición:** Cubo de contadores de un limitador asignado a una clave (IP, usuario, llave de API) para que un cliente no agote el límite de los demás.
**En el curso:** Parte 4.

### Problema HTTP (`application/problem+json`)
**Categoría:** RFC 9457
**Definición:** Formato estándar de error para APIs HTTP, con `type`, `title`, `status`, `detail` e `instance`. Reemplaza al RFC 7807.
**En el curso:** Parte 3.

### ProblemDetails
**Categoría:** ASP.NET Core
**Definición:** Clase de ASP.NET Core que representa un problema HTTP del RFC 9457.
**En el curso:** Parte 3.

---

## R

### Retry-After
**Categoría:** HTTP
**Definición:** Cabecera que dice cuánto esperar antes de reintentar; el RFC 6585 permite incluirla en un 429.
**En el curso:** Parte 4.

---

## S

### Sliding window (ventana deslizante)
**Categoría:** Límites
**Definición:** Ventana de tiempo dividida en segmentos que se desliza; reparte mejor que la ventana fija.
**En el curso:** Parte 4.

---

## T

### traceId
**Categoría:** Operación
**Definición:** Identificador de la petición que sale en el problema HTTP y en la bitácora, para encontrar el detalle del error.
**En el curso:** Parte 3.

---

## V

### Validación en el servidor
**Categoría:** Defensa
**Definición:** Comprobación de las entradas dentro de la API, aunque el cliente ya las haya validado.
**En el curso:** Parte 1.

---

*Rogelio Arriaga Gonzalez*
