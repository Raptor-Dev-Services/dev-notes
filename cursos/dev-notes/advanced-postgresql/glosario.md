# Glosario Técnico  -  PostgreSQL Avanzado

> Definiciones de todos los términos técnicos del módulo.
> Ordenado alfabéticamente.

---

## A

### ACID
**Categoría:** Base de datos / Transacciones
**Definición:** Acrónimo de las cuatro propiedades que garantizan la fiabilidad de una transacción de base de datos: Atomicidad, Consistencia, Aislamiento (Isolation) y Durabilidad. Son el contrato que PostgreSQL (y cualquier RDBMS de producción) cumple para que los datos sean confiables incluso ante fallos del sistema.
**En el proyecto:** Toda asignación de conversación en el CRM (UPDATE + INSERT de mensaje de sistema) debe ocurrir dentro de una transacción ACID para que nunca quede una conversación con agente asignado pero sin el mensaje de auditoría correspondiente.

### Aislamiento (Isolation)
**Categoría:** Base de datos / Transacciones / ACID
**Definición:** Propiedad ACID que garantiza que las operaciones de una transacción concurrente no son visibles para otras transacciones hasta que se hace COMMIT. Su nivel de aplicación se controla con los Isolation Levels. Sin aislamiento, dos agentes podrían tomar la misma conversación simultáneamente.
**En el proyecto:** En el CRM, dos agentes no deben ver el mismo estado "sin asignar" de una conversación si otro agente ya la está tomando. El nivel `READ COMMITTED` (default de PostgreSQL) es suficiente para la mayoría de las operaciones del sistema de mensajería.

### ANY (operador)
**Categoría:** PostgreSQL / SQL
**Definición:** Operador que comprueba si un valor es igual a alguno de los elementos de un array. Se usa en la cláusula WHERE para filtrar filas cuya columna de tipo array contiene un valor específico. Es el mecanismo estándar para consultar columnas TEXT[].
**En el proyecto:** Filtra contactos por etiqueta: `WHERE 'vip' = ANY(tags)`. Permite segmentar la base de contactos del CRM sin necesidad de una tabla pivot separada.

### ANALYZE
**Categoría:** PostgreSQL / Mantenimiento
**Definición:** Comando que actualiza las estadísticas internas de distribución de datos de una tabla (histogramas, cardinalidades). El query planner de PostgreSQL usa estas estadísticas para decidir qué plan de ejecución usar. Sin estadísticas actualizadas, el planner puede tomar decisiones subóptimas.
**En el proyecto:** Se ejecuta tras cargas masivas de contactos (importación CSV) o mensajes para que el planner elija correctamente entre Index Scan y Seq Scan en las queries de la bandeja.

### Array type (TEXT[])
**Categoría:** PostgreSQL / Tipos de dato
**Definición:** Tipo de columna nativo de PostgreSQL que almacena una lista ordenada de valores del mismo tipo en una sola celda. Evita crear tablas de relación N:M para conjuntos pequeños y estables. Soporta índices GIN para búsquedas eficientes de membresía.
**En el proyecto:** La columna `tags TEXT[]` en la tabla `contacts` almacena las etiquetas (vip, recurrente, mexico) de cada contacto, permitiendo segmentación sin una tabla separada `contact_tags`.

### Atomicidad
**Categoría:** Base de datos / Transacciones / ACID
**Definición:** Propiedad ACID que garantiza que todas las operaciones de una transacción se ejecutan como una unidad indivisible: o todas tienen éxito y se confirman con COMMIT, o ninguna se aplica y el sistema hace ROLLBACK automático. No existe estado intermedio visible.
**En el proyecto:** Cuando el CRM asigna una conversación, el UPDATE del agente y el INSERT del mensaje de sistema son atómicos. Si falla el INSERT, el UPDATE también se revierte, evitando inconsistencias.

### autovacuum
**Categoría:** PostgreSQL / Mantenimiento
**Definición:** Proceso en segundo plano de PostgreSQL que ejecuta VACUUM y ANALYZE automáticamente sobre tablas con alto volumen de dead tuples o filas sin estadísticas actualizadas. Se configura a nivel de tabla o globalmente en `postgresql.conf`. Reduce la necesidad de mantenimiento manual.
**En el proyecto:** En producción, `autovacuum` mantendrá limpias las tablas `messages` y `conversations` que reciben muchos UPDATE (cambios de estado). Solo es necesario intervenir manualmente si los dead tuples crecen demasiado rápido.

### AVG OVER
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que calcula el promedio de un conjunto de filas definido por la cláusula OVER (con o sin PARTITION BY y framing). A diferencia de AVG con GROUP BY, no colapsa filas: cada fila conserva su propia información y adicionalmente contiene el promedio calculado sobre la ventana.
**En el proyecto:** Calcula el promedio móvil de conversaciones cerradas por agente en los últimos 7 días (`ROWS BETWEEN 6 PRECEDING AND CURRENT ROW`) para mostrar tendencias en el dashboard del CRM.

---

## B

### B-tree (índice)
**Categoría:** PostgreSQL / Índices
**Definición:** Tipo de índice por defecto en PostgreSQL, implementado como un árbol binario equilibrado. Soporta operaciones de igualdad, rangos (`<`, `>`, `BETWEEN`), ordenamiento (`ORDER BY`) y prefijos de texto (`LIKE 'abc%'`). Es la opción correcta para la mayoría de las columnas escalares de uso general.
**En el proyecto:** Índice principal sobre `email`, `created_at` y `status` en las tablas `contacts` y `conversations`. Permite búsquedas por correo y paginación eficiente por fecha.

### BEGIN
**Categoría:** PostgreSQL / SQL / Transacciones
**Definición:** Comando SQL que inicia una transacción explícita. A partir de BEGIN, todas las sentencias posteriores forman parte de la misma unidad atómica hasta que se ejecuta COMMIT o ROLLBACK. Sin BEGIN, cada sentencia es su propia transacción (auto-commit).
**En el proyecto:** Se usa en Dapper a través de `conn.BeginTransactionAsync()`, que genera el BEGIN implícito antes de ejecutar el UPDATE de asignación y el INSERT del mensaje de sistema.

### BRIN (Block Range INdex)
**Categoría:** PostgreSQL / Índices
**Definición:** Tipo de índice muy compacto diseñado para tablas enormes donde los datos están físicamente correlacionados con su orden de inserción (por ejemplo, una columna `created_at` que crece cronológicamente). Almacena solo el rango mínimo-máximo de valores por bloque de disco, no un puntero por fila.
**En el proyecto:** Aplicable a tablas de auditoría o logs del CRM ordenados por fecha donde el índice B-tree estándar sería demasiado grande y costoso de mantener.

---

## C

### Cardinalidad
**Categoría:** Base de datos / Índices
**Definición:** Número de valores distintos que tiene una columna. Alta cardinalidad (muchos valores únicos, como `email`) hace que un índice sea muy efectivo. Baja cardinalidad (pocos valores distintos, como una columna booleana o `status` con 3 valores) hace que el índice sea ineficiente porque la mayoría de queries igual leerán muchas filas.
**En el proyecto:** Crear un índice B-tree sobre una columna booleana en una tabla de 10M de filas no ayuda: el planner prefiere Seq Scan porque casi todas las filas tienen el mismo valor. En el CRM, `status` tiene baja cardinalidad pero se compensa con índices parciales.

### Cold start
**Categoría:** Backend / Performance
**Definición:** Latencia adicional que ocurre cuando una aplicación necesita establecer conexiones nuevas a la base de datos porque el pool está vacío. Con `Minimum Pool Size = 0`, las primeras peticiones tras un reinicio incurren en este costo. Configurar un mínimo de conexiones precalentadas lo elimina.
**En el proyecto:** El CRM configura `Minimum Pool Size=5` en la connection string de Npgsql para que el pod de Docker ya tenga conexiones activas desde el arranque y no sufra latencia en las primeras requests.

### Command Timeout
**Categoría:** PostgreSQL / Npgsql / Connection Pooling
**Definición:** Tiempo máximo en segundos que Npgsql espera una respuesta del servidor PostgreSQL antes de cancelar la query y lanzar una excepción. El valor por defecto es 30 segundos. Queries que superen ese tiempo se cancelan automáticamente, protegiendo el pool de conexiones bloqueadas.
**En el proyecto:** Las queries de importación masiva de contactos o generación de reportes pueden requerir un timeout mayor. Se ajusta por query o globalmente en la connection string del CRM.

### COMMIT
**Categoría:** PostgreSQL / SQL / Transacciones
**Definición:** Comando que confirma permanentemente todos los cambios realizados desde el BEGIN. Tras COMMIT, los datos son visibles para otras sesiones y sobreviven a reinicios del servidor (Durabilidad). Si la aplicación cae antes del COMMIT, PostgreSQL revierte los cambios automáticamente.
**En el proyecto:** En el patrón Dapper del CRM, `await tx.CommitAsync(ct)` confirma la asignación de conversación solo si ambas operaciones (UPDATE + INSERT) se completaron sin errores.

### Connection Idle Lifetime
**Categoría:** PostgreSQL / Npgsql / Connection Pooling
**Definición:** Tiempo en segundos que una conexión puede permanecer en el pool sin ser utilizada antes de ser cerrada y descartada. Evita mantener conexiones abiertas innecesariamente cuando el tráfico baja. El valor recomendado es 300 segundos (5 minutos).
**En el proyecto:** Parámetro de la connection string del sistema de mensajería. Durante horas de baja actividad, el pool reduce su tamaño cerrando conexiones inactivas sin afectar la disponibilidad.

### Connection Pooling
**Categoría:** PostgreSQL / Backend / Performance
**Definición:** Técnica que reutiliza conexiones de base de datos ya establecidas en lugar de abrir y cerrar una conexión nueva por cada request. Establecer una conexión PostgreSQL es costoso (handshake TCP + autenticación). El pool mantiene un conjunto de conexiones listas para usar, mejorando drásticamente el throughput.
**En el proyecto:** Npgsql gestiona el pool automáticamente a partir de la connection string. El CRM nunca debe crear `NpgsqlConnection` con `new` fuera del factory, ya que eso rompe el pool y genera una nueva conexión por request.

### Consistencia (Consistency)
**Categoría:** Base de datos / Transacciones / ACID
**Definición:** Propiedad ACID que garantiza que toda transacción lleva la base de datos de un estado válido a otro estado válido, respetando todas las restricciones definidas (constraints, foreign keys, checks). No puede quedar un estado parcialmente correcto.
**En el proyecto:** Un mensaje no puede existir sin su conversación (FK constraint). Si se intenta insertar un mensaje con un `conversation_id` inválido, PostgreSQL rechaza la operación y mantiene la consistencia del esquema del CRM.

### Correlated Subquery (Subquery correlacionada)
**Categoría:** SQL / PostgreSQL
**Definición:** Subquery que referencia columnas de la query exterior (outer query). Se re-ejecuta una vez por cada fila del outer query, lo que la hace potencialmente costosa. Es funcional pero generalmente reemplazable por un JOIN o window function para mejor performance.
**En el proyecto:** Contar el total de conversaciones de cada contacto: `(SELECT COUNT(*) FROM conversations WHERE contact_id = c.id)`. Funciona bien para tablas pequeñas; con millones de contactos conviene reemplazarla por un LEFT JOIN con GROUP BY.

### CTE (Common Table Expression)
**Categoría:** SQL / PostgreSQL
**Definición:** Subconsulta nombrada y reutilizable definida con la cláusula `WITH` antes del SELECT principal. Permite descomponer queries complejos en pasos lógicos legibles, dar nombre a resultados intermedios y referenciarlos múltiples veces. Son equivalentes a vistas temporales de la sesión.
**En el proyecto:** La bandeja de entrada del CRM usa CTEs encadenadas: `active_contacts`, `last_message` y `open_conversations` para obtener en una sola query las conversaciones abiertas con su último mensaje y los datos del contacto.

### CTE encadenada
**Categoría:** SQL / PostgreSQL
**Definición:** Uso de múltiples CTEs en secuencia dentro de un mismo bloque WITH, donde cada CTE puede referenciar a las anteriores. Permite construir queries complejos paso a paso, similar a una pipeline de transformación de datos, con cada paso legible e independientemente testeable.
**En el proyecto:** La query de la bandeja del CRM encadena tres CTEs: primero filtra contactos activos, luego obtiene el último mensaje por conversación con `DISTINCT ON`, y finalmente une todo para producir el listado ordenado por actividad reciente.

### Cursor-based pagination
**Categoría:** Backend / SQL / Performance
**Definición:** Técnica de paginación que usa el valor del último elemento visto (cursor) como punto de partida para la siguiente página, en lugar de OFFSET. Evita el problema de rendimiento del OFFSET alto y es más consistente cuando se insertan nuevas filas. La query usa `WHERE created_at < @lastCursor ORDER BY created_at DESC LIMIT N`.
**En el proyecto:** La bandeja de entrada del CRM usa paginación cursor-based sobre `created_at` para que la carga de la siguiente página no degrade con el tiempo aunque la tabla tenga millones de conversaciones.

---

## D

### Dapper
**Categoría:** Backend / .NET / ORM
**Definición:** Micro-ORM para .NET que extiende `IDbConnection` con métodos como `QueryAsync`, `ExecuteAsync` y `QueryMultipleAsync`. Mapea resultados SQL directamente a tipos C# con alias de columna (`AS NombrePropiedad`). Es más rápido que EF Core para queries complejas y explícitas, y no abstrae el SQL.
**En el proyecto:** El CRM usa Dapper a través de `MainDapperDbConnection` para todas las queries de lectura complejas (bandeja paginada, búsqueda FTS, reportes) donde el SQL explícito es preferible a las queries generadas por EF Core.

### date_trunc
**Categoría:** PostgreSQL / Funciones
**Definición:** Función de PostgreSQL que trunca un valor de tipo timestamp al nivel de precisión indicado (day, hour, week, month, year). Muy útil para agrupar datos temporales en intervalos regulares sin perder el tipo de dato.
**En el proyecto:** Agrupa los mensajes del CRM por día (`date_trunc('day', sent_at)`) para calcular métricas de volumen diario de mensajes y graficarlas en el dashboard de reporting.

### Dead tuples
**Categoría:** PostgreSQL / MVCC / Mantenimiento
**Definición:** Versiones antiguas de filas que ya no son visibles para ninguna transacción activa pero que siguen ocupando espacio en disco. Se generan por cada UPDATE y DELETE debido al modelo MVCC de PostgreSQL. Se limpian mediante VACUUM.
**En el proyecto:** Las tablas `conversations` y `messages` del CRM reciben muchos UPDATE (cambios de estado, asignaciones). Sin VACUUM periódico, acumularían dead tuples que inflarían el tamaño en disco y degradarían el rendimiento.

### Deadlock
**Categoría:** Base de datos / Transacciones
**Definición:** Situación en la que dos o más transacciones se bloquean mutuamente esperando locks que la otra tiene. Ninguna puede avanzar. PostgreSQL detecta el ciclo automáticamente y termina la transacción más barata de reintentar, lanzando un error a la aplicación.
**En el proyecto:** Puede ocurrir si dos requests del CRM intentan actualizar `conversations` y `contacts` en orden diferente. La solución es siempre adquirir locks en el mismo orden (primero conversación, luego contacto) usando `SELECT ... FOR UPDATE`.

### DENSE_RANK
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que asigna rangos consecutivos sin huecos, incluso cuando hay empates. Si dos filas empatan en posición 1, la siguiente fila recibe posición 2 (no 3 como en RANK). Útil cuando se necesita un ranking compacto sin saltos.
**En el proyecto:** Ranking de contactos por volumen de mensajes donde no se quieren huecos en la numeración, por ejemplo para mostrar "Top 10 contactos más activos" sin que el número salte de 2 a 4 por un empate.

### Derived table (tabla derivada)
**Categoría:** SQL / PostgreSQL
**Definición:** Subquery ubicada en la cláusula FROM que actúa como una tabla temporal con alias. A diferencia de las CTEs, no tiene nombre reutilizable y debe estar completamente dentro del FROM. Equivalente funcional a una CTE pero menos legible cuando hay múltiples niveles de anidamiento.
**En el proyecto:** Usado en la query de estadísticas recientes de agentes: `FROM (SELECT agent_id, COUNT(*) AS total FROM conversations WHERE created_at > NOW() - INTERVAL '30 days' GROUP BY agent_id) AS recent_stats`.

### Dirty Read
**Categoría:** Base de datos / Transacciones / Isolation Levels
**Definición:** Anomalía de concurrencia donde una transacción lee datos modificados por otra transacción que aún no hizo COMMIT y que podría hacer ROLLBACK. Los datos leídos nunca existieron oficialmente. PostgreSQL no permite dirty reads ni siquiera en el nivel más bajo de aislamiento.
**En el proyecto:** No ocurre en el CRM porque PostgreSQL usa `READ COMMITTED` como mínimo. Un agente nunca verá una conversación "asignada" a otro agente si ese agente aún no confirmó la transacción.

### DISTINCT ON
**Categoría:** PostgreSQL / SQL
**Definición:** Extensión de PostgreSQL (no estándar SQL) que devuelve exactamente una fila por grupo de valores de la columna indicada, eligiendo la fila determinada por el ORDER BY. Es más eficiente que `ROW_NUMBER() OVER (...) WHERE rn = 1` para este patrón.
**En el proyecto:** La CTE `last_message` de la bandeja usa `DISTINCT ON (conversation_id) ORDER BY conversation_id, sent_at DESC` para obtener el último mensaje de cada conversación en una sola pasada eficiente.

### Durabilidad (Durability)
**Categoría:** Base de datos / Transacciones / ACID
**Definición:** Propiedad ACID que garantiza que los cambios confirmados con COMMIT persisten permanentemente, incluso ante fallos del servidor, cortes de electricidad o reinicios. PostgreSQL usa el WAL (Write-Ahead Log) para garantizar esta propiedad.
**En el proyecto:** Un mensaje enviado por un agente del CRM que recibe COMMIT nunca desaparece aunque el servidor se reinicie segundos después. El WAL de PostgreSQL garantiza la escritura en disco antes de confirmar.

### DynamicParameters
**Categoría:** Dapper / .NET
**Definición:** Clase de Dapper que permite construir colecciones de parámetros SQL de forma dinámica en tiempo de ejecución, agregando parámetros con nombre y valor mediante `Add()`. Reemplaza los objetos anónimos cuando el número de parámetros varía según la lógica de la aplicación.
**En el proyecto:** Construye el WHERE dinámico del buscador de contactos del CRM: si se proporciona `name`, se agrega `@name`; si se proporciona `tag`, se agrega `@tag`; nunca se concatenan strings con valores de usuario.

---

## E

### ExecuteAsync
**Categoría:** Dapper / .NET
**Definición:** Método de Dapper para ejecutar sentencias SQL que no devuelven filas (INSERT, UPDATE, DELETE, DDL). Retorna el número de filas afectadas como `int`. Acepta parámetros tipados para prevenir SQL injection.
**En el proyecto:** Ejecuta el UPDATE de asignación de agente y el INSERT del mensaje de sistema dentro de la transacción Dapper del CRM. El valor de retorno confirma que la fila fue modificada.

### ExecuteScalarAsync
**Categoría:** Dapper / .NET
**Definición:** Método de Dapper que ejecuta una query y retorna el valor de la primera columna de la primera fila, tipado genéricamente. Ideal para COUNT(*), EXISTS, MAX, MIN y cualquier valor escalar agregado.
**En el proyecto:** Obtiene el total de conversaciones para la paginación del CRM (`SELECT COUNT(*) ...`) sin necesidad de mapear a un DTO completo. También evalúa existencia (`bool exists = await _db.ExecuteScalarAsync<bool>(...)`).

### EXPLAIN ANALYZE
**Categoría:** PostgreSQL / Performance / Diagnóstico
**Definición:** Comando de PostgreSQL que muestra el plan de ejecución real de una query (con tiempos medidos de ejecución, no estimados). A diferencia de EXPLAIN solo, EXPLAIN ANALYZE ejecuta la query y reporta el costo real de cada nodo del plan, permitiendo identificar dónde está el cuello de botella.
**En el proyecto:** Herramienta de diagnóstico primaria del CRM. Cuando una query de la bandeja o búsqueda de contactos es lenta, se ejecuta EXPLAIN ANALYZE para ver si hay un Seq Scan inesperado o un Nested Loop costoso.

---

## F

### FOR UPDATE
**Categoría:** PostgreSQL / SQL / Transacciones
**Definición:** Cláusula que adquiere un lock exclusivo sobre las filas seleccionadas por un SELECT. Las filas bloqueadas no pueden ser modificadas por otras transacciones hasta que la transacción actual haga COMMIT o ROLLBACK. Se usa para prevenir condiciones de carrera en actualizaciones concurrentes.
**En el proyecto:** Previene deadlocks en el CRM al adquirir locks sobre conversaciones y contactos en orden consistente antes de los UPDATE, evitando que dos requests procesen el mismo recurso simultáneamente.

### Foreign key (FK)
**Categoría:** Base de datos / SQL
**Definición:** Restricción de integridad referencial que garantiza que el valor de una columna en una tabla existe como clave primaria en otra tabla. PostgreSQL rechaza inserciones o actualizaciones que violen esta restricción. Las foreign keys siempre deben tener un índice en la columna referenciante para evitar Seq Scans en JOINs.
**En el proyecto:** `messages.conversation_id` referencia `conversations.id`. Un mensaje no puede existir sin su conversación. El índice en `conversation_id` es obligatorio para que los JOINs de la bandeja sean eficientes.

### Full Text Search (FTS)
**Categoría:** PostgreSQL / Búsqueda
**Definición:** Capacidad nativa de PostgreSQL para búsqueda de texto completo con tokenización, stemming (normalización de palabras a su raíz) y ranking por relevancia. Se basa en dos tipos: `tsvector` (documento indexado) y `tsquery` (patrón de búsqueda). Soporta múltiples idiomas incluyendo español.
**En el proyecto:** La bandeja del CRM permite buscar mensajes que contengan palabras como "factura" o "pago" usando `to_tsvector('spanish', body) @@ to_tsquery('spanish', 'factura | pago')`, con índice GIN para performance.

---

## G

### GIN (Generalized Inverted Index)
**Categoría:** PostgreSQL / Índices
**Definición:** Tipo de índice optimizado para columnas que contienen múltiples valores por fila, como arrays, JSONB y documentos de texto completo (tsvector). Almacena una entrada por valor dentro del array/objeto, permitiendo búsquedas de membresía extremadamente rápidas.
**En el proyecto:** El CRM usa índices GIN sobre `metadata JSONB` (para el operador `@>`), sobre `to_tsvector('spanish', body)` (para FTS en mensajes) y sobre `tags TEXT[]` (para `ANY(tags)`).

### GiST (Generalized Search Tree)
**Categoría:** PostgreSQL / Índices
**Definición:** Framework de índice extensible que permite indexar tipos de datos complejos como coordenadas geoespaciales, rangos solapados y datos geométricos. Es la base de la extensión PostGIS para datos geográficos. Menos común que B-tree y GIN para aplicaciones web estándar.
**En el proyecto:** Sería aplicable si el CRM almacenara coordenadas de ubicación de contactos para búsquedas por proximidad geográfica (contactos cercanos a una sucursal).

### GROUP BY
**Categoría:** SQL / PostgreSQL
**Definición:** Cláusula que colapsa múltiples filas con el mismo valor en la columna agrupada en una sola fila, permitiendo aplicar funciones de agregación (COUNT, SUM, AVG). A diferencia de las window functions, el resultado pierde el detalle de fila individual.
**En el proyecto:** Agrupa conversaciones por `agent_id` para contar cuántas conversaciones abiertas tiene cada agente. Cuando se necesita mantener el detalle de cada conversación además del conteo, se usa una window function en su lugar.

---

## H

### HASH (índice)
**Categoría:** PostgreSQL / Índices
**Definición:** Tipo de índice que solo soporta igualdad exacta (`=`). Teóricamente más rápido que B-tree para igualdad pura, pero en práctica el B-tree es competitivo y soporta más operaciones. No soporta rangos ni ORDER BY. Raramente justificado en aplicaciones modernas con PostgreSQL.
**En el proyecto:** Se evita en el CRM. En todos los casos donde se consideraría HASH, un índice B-tree ofrece la misma velocidad de igualdad con la ventaja adicional de soportar ORDER BY y LIKE de prefijo.

---

## I

### ILIKE
**Categoría:** PostgreSQL / SQL
**Definición:** Operador de comparación de texto case-insensitive (sin distinción entre mayúsculas y minúsculas), equivalente a LIKE pero ignorando el caso. Específico de PostgreSQL. Usado con `%` como comodín para búsquedas de substring.
**En el proyecto:** La búsqueda de contactos por nombre en el CRM usa `name ILIKE @name` con el parámetro formateado como `%{name}%`, permitiendo encontrar "Ana", "ana" o "ANA" con la misma query.

### Index Only Scan
**Categoría:** PostgreSQL / Índices / EXPLAIN
**Definición:** Nodo de plan de ejecución donde PostgreSQL satisface la query leyendo únicamente el índice, sin acceder a la tabla principal en heap. Es el tipo de scan más eficiente posible. Ocurre cuando todas las columnas requeridas por la query están incluidas en el índice.
**En el proyecto:** Una query que solo selecciona `agent_id` de `conversations` usando un índice sobre esa columna puede resultar en Index Only Scan, sin necesidad de leer las filas reales de la tabla.

### Index Scan
**Categoría:** PostgreSQL / Índices / EXPLAIN
**Definición:** Nodo de plan de ejecución donde PostgreSQL navega el índice para encontrar los punteros a las filas que satisfacen la condición, y luego accede a esas filas específicas en el heap. Mucho más eficiente que Seq Scan para tablas grandes con alta selectividad.
**En el proyecto:** El nodo esperado en EXPLAIN ANALYZE para queries de bandeja filtradas por `agent_id` y `status`. Si aparece Seq Scan en una tabla con más de 10,000 filas, falta un índice.

### Índice compuesto
**Categoría:** PostgreSQL / Índices
**Definición:** Índice que abarca dos o más columnas. Es efectivo para queries que filtran por la primera columna y opcionalmente por las siguientes. La regla clave es que la primera columna debe ser la de mayor selectividad (más valores distintos) y debe coincidir con las condiciones del WHERE.
**En el proyecto:** El índice `(agent_id, created_at DESC)` en `conversations` permite la paginación de la bandeja: filtra eficientemente por agente y ordena por fecha sin scan adicional.

### Índice parcial
**Categoría:** PostgreSQL / Índices
**Definición:** Índice que solo incluye las filas que cumplen una condición WHERE definida al momento de crear el índice. Es más pequeño que un índice completo, cabe mejor en caché y se mantiene más rápido. Solo puede ser usado por queries cuya cláusula WHERE incluye exactamente la condición del índice parcial.
**En el proyecto:** `CREATE INDEX ... WHERE deleted_at IS NULL` en `conversations` y `contacts`. Como el 95% de las queries del CRM filtran por `deleted_at IS NULL` (soft delete), este índice es mucho más efectivo que indexar todas las filas incluyendo las eliminadas.

### INNER JOIN
**Categoría:** SQL / PostgreSQL
**Definición:** Tipo de JOIN que devuelve únicamente las filas que tienen coincidencia en ambas tablas según la condición ON. Si una fila de la tabla izquierda no tiene coincidencia en la derecha (o viceversa), se omite del resultado.
**En el proyecto:** Obtiene conversaciones que tienen agente asignado: `FROM conversations c INNER JOIN agents a ON a.id = c.agent_id`. Las conversaciones sin agente (`agent_id IS NULL`) no aparecen en el resultado.

### INTERVAL
**Categoría:** PostgreSQL / Tipos de dato / Funciones
**Definición:** Tipo de dato que representa una duración de tiempo. Se usa en expresiones aritméticas con fechas para calcular rangos dinámicos (`NOW() - INTERVAL '30 days'`). Evita calcular fechas en código de aplicación y las pasa como parámetros.
**En el proyecto:** Filtra conversaciones y mensajes de los últimos 30 días para el dashboard de métricas del CRM: `WHERE created_at > NOW() - INTERVAL '30 days'`.

### Isolation Level
**Categoría:** Base de datos / Transacciones
**Definición:** Configuración que controla cuánto se aísla una transacción de los cambios realizados por otras transacciones concurrentes. PostgreSQL define cuatro niveles: READ UNCOMMITTED (igual a READ COMMITTED en Postgres), READ COMMITTED (default), REPEATABLE READ y SERIALIZABLE. Mayor aislamiento equivale a mayor seguridad pero menor concurrencia.
**En el proyecto:** El CRM usa el nivel por defecto `READ COMMITTED`. Solo se escalaría a `SERIALIZABLE` para operaciones financieras críticas como cobros de suscripción, que no aplican en la versión actual del sistema de mensajería.

---

## J

### JOIN (general)
**Categoría:** SQL / PostgreSQL
**Definición:** Operación SQL que combina filas de dos o más tablas basándose en una condición de relación (normalmente una igualdad de clave). Existen varios tipos: INNER, LEFT, RIGHT, CROSS y LATERAL. El tipo de JOIN determina qué filas se incluyen cuando no hay coincidencia.
**En el proyecto:** Todas las queries principales del CRM usan JOINs: conversaciones con contactos y agentes para la bandeja, mensajes con conversaciones para el historial, etc.

### JSONB
**Categoría:** PostgreSQL / Tipos de dato
**Definición:** Tipo de columna que almacena datos JSON en formato binario descompuesto. A diferencia de JSON (texto plano), JSONB elimina espacios redundantes, ordena claves y permite indexación con GIN. Soporta operadores especiales (`->`, `->>`, `@>`, `?`) para acceder y filtrar por campos internos sin extraer el documento completo.
**En el proyecto:** La columna `metadata JSONB` en `contacts` almacena datos variables por canal (whatsapp_id, instagram_handle, score) que no requieren columnas propias. Es más flexible que una tabla de atributos EAV y más eficiente que JSON texto.

---

## L

### LAG
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que retorna el valor de una columna de la fila anterior dentro de la partición y orden definidos por OVER. Si no hay fila anterior (primera fila de la partición), retorna NULL. Útil para calcular diferencias entre filas consecutivas.
**En el proyecto:** Calcula el tiempo transcurrido entre mensajes consecutivos en una conversación del CRM (`sent_at - LAG(sent_at) OVER (...)`), lo que permite medir tiempos de respuesta de agentes.

### LATERAL
**Categoría:** PostgreSQL / SQL
**Definición:** Palabra clave que permite a una subquery en el FROM referenciar columnas de tablas anteriores del mismo FROM, como si fuera un bucle correlacionado que se ejecuta una vez por fila de la tabla izquierda. Equivalente a un JOIN correlacionado eficiente.
**En el proyecto:** La query paginada de la bandeja usa `LEFT JOIN LATERAL (SELECT body, sent_at FROM messages WHERE conversation_id = c.id ORDER BY sent_at DESC LIMIT 1) m ON true` para obtener el último mensaje de cada conversación eficientemente.

### LEAD
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que retorna el valor de una columna de la fila siguiente dentro de la partición y orden definidos por OVER. Si no hay fila siguiente (última fila de la partición), retorna NULL. Complementario a LAG.
**En el proyecto:** Junto a LAG, permite analizar el flujo de mensajes en una conversación del CRM: cuánto falta para el siguiente mensaje de respuesta (`LEAD(sent_at)`) para detectar conversaciones con tiempos de respuesta altos.

### LEFT JOIN
**Categoría:** SQL / PostgreSQL
**Definición:** Tipo de JOIN que devuelve todas las filas de la tabla izquierda, con los valores de la tabla derecha cuando hay coincidencia, o NULL cuando no la hay. Garantiza que ninguna fila de la tabla izquierda se omita del resultado.
**En el proyecto:** La bandeja del CRM usa LEFT JOIN para incluir todas las conversaciones abiertas aunque no tengan un último mensaje (conversaciones recién creadas): `LEFT JOIN last_message lm ON lm.conversation_id = c.id`.

### LEFT JOIN LATERAL
**Categoría:** PostgreSQL / SQL
**Definición:** Combinación de LEFT JOIN y LATERAL que incluye todas las filas de la tabla izquierda aunque la subquery LATERAL no retorne filas, poniendo NULL en las columnas de la subquery. Es el patrón estándar para "el último elemento de cada grupo" en PostgreSQL.
**En el proyecto:** Obtiene el último mensaje de cada conversación en la query paginada de la bandeja del CRM, sin excluir conversaciones que aún no tienen mensajes.

### LIMIT
**Categoría:** SQL / PostgreSQL / Paginación
**Definición:** Cláusula que restringe el número máximo de filas devueltas por una query. Se combina con OFFSET para paginación por número de página, o con ORDER BY + cursor para paginación cursor-based. Siempre debe acompañarse de ORDER BY para resultados deterministas.
**En el proyecto:** Todas las queries paginadas del CRM usan LIMIT con el tamaño de página (`@pageSize`) para que la bandeja no devuelva miles de conversaciones en una sola respuesta.

---

## M

### Maximum Pool Size
**Categoría:** PostgreSQL / Npgsql / Connection Pooling
**Definición:** Número máximo de conexiones simultáneas que el pool de Npgsql puede mantener abiertas hacia PostgreSQL. Si todas las conexiones están en uso y llega un nuevo request, espera hasta que una se libere. Valor por defecto: 100. Debe coincidir con `max_connections` configurado en PostgreSQL.
**En el proyecto:** El CRM configura `Maximum Pool Size=100` para una instancia. Si se escala horizontalmente con múltiples pods, cada pod tendrá su propio pool, por lo que el `max_connections` de PostgreSQL debe ser al menos `pods × pool_size`.

### Minimum Pool Size
**Categoría:** PostgreSQL / Npgsql / Connection Pooling
**Definición:** Número mínimo de conexiones que el pool mantiene abiertas incluso durante períodos de inactividad. Elimina el cold start al garantizar que siempre hay conexiones listas para usar desde el arranque de la aplicación.
**En el proyecto:** El CRM configura `Minimum Pool Size=5`. Al arrancar el contenedor Docker, Npgsql pre-establece 5 conexiones para que las primeras requests de usuarios no sufran latencia adicional.

### MVCC (Multi-Version Concurrency Control)
**Categoría:** PostgreSQL / Concurrencia
**Definición:** Mecanismo interno de PostgreSQL para gestionar transacciones concurrentes sin bloquear lectores. En lugar de bloquear filas al modificarlas, crea nuevas versiones de la fila. Los lectores ven la versión más reciente commiteada según su snapshot. El costo es la acumulación de dead tuples que requieren VACUUM.
**En el proyecto:** Permite que los agentes del CRM lean la bandeja sin bloquear a los agentes que están asignando conversaciones. Las lecturas nunca bloquean escrituras ni viceversa.

### Multi-resultado (QueryMultipleAsync)
**Categoría:** Dapper / .NET / Performance
**Definición:** Capacidad de Dapper para ejecutar múltiples sentencias SQL separadas por `;` en una sola round-trip a la base de datos y leer cada conjunto de resultados de forma independiente. Reduce la latencia de red al evitar múltiples viajes para datos relacionados.
**En el proyecto:** La query de bandeja paginada del CRM obtiene en una sola llamada los ítems de la página actual (`SELECT ... LIMIT @pageSize OFFSET @offset`) y el total de registros (`SELECT COUNT(*)`) para el componente de paginación del frontend.

---

## N

### Nested Loop
**Categoría:** PostgreSQL / EXPLAIN / Plan de ejecución
**Definición:** Nodo de plan de ejecución que implementa un JOIN iterando sobre cada fila de la tabla exterior y buscando coincidencias en la tabla interior. Eficiente cuando la tabla exterior es pequeña y la interior tiene un índice. Se vuelve costoso (O(n×m)) cuando ambas tablas son grandes.
**En el proyecto:** En EXPLAIN ANALYZE de la bandeja del CRM, un Nested Loop sobre `conversations × messages` sin índice en `messages.conversation_id` es una señal de que falta el índice en esa FK.

### Non-repeatable Read
**Categoría:** Base de datos / Transacciones / Isolation Levels
**Definición:** Anomalía de concurrencia donde una transacción lee la misma fila dos veces y obtiene valores diferentes porque otra transacción la modificó y confirmó entre las dos lecturas. Posible en el nivel READ COMMITTED, prevenida en REPEATABLE READ.
**En el proyecto:** En `READ COMMITTED`, si un agente leyera el estado de una conversación dos veces en la misma transacción, podría ver el estado cambiar si otro agente lo modificó. Para la mayoría de las operaciones del CRM esto es aceptable.

### NOW()
**Categoría:** PostgreSQL / Funciones
**Definición:** Función de PostgreSQL que retorna el timestamp actual con zona horaria (timestamptz) del inicio de la transacción. Todos los NOW() dentro de la misma transacción retornan el mismo valor, garantizando consistencia temporal. Alternativa: `timezone('utc', now())` para obtener UTC explícito.
**En el proyecto:** El CRM usa `timezone('utc', now())` en todos los INSERT y UPDATE para almacenar fechas siempre en UTC, evitando problemas de zona horaria al leer desde diferentes regiones.

### Npgsql
**Categoría:** .NET / PostgreSQL / Driver
**Definición:** Driver oficial de .NET para PostgreSQL. Implementa ADO.NET y gestiona el connection pool automáticamente. Compatible con Dapper y EF Core como provider. Soporta configuración de pool avanzada a través de la connection string (Minimum Pool Size, Maximum Pool Size, Command Timeout, etc.).
**En el proyecto:** Dependencia de infraestructura del sistema de mensajería. Toda comunicación entre la API .NET 10 y PostgreSQL pasa por Npgsql, incluyendo la gestión del pool de conexiones.

### NULLS LAST
**Categoría:** PostgreSQL / SQL
**Definición:** Modificador de ORDER BY que coloca las filas con valor NULL al final del resultado ordenado. Por defecto, PostgreSQL coloca los NULL al final en ORDER BY DESC y al inicio en ORDER BY ASC. `NULLS LAST` fuerza siempre que los NULL vayan al final independientemente de la dirección.
**En el proyecto:** La bandeja del CRM ordena por `last_activity DESC NULLS LAST` para que las conversaciones sin mensajes (actividad NULL) aparezcan al final, no al inicio de la lista.

---

## O

### OFFSET
**Categoría:** SQL / PostgreSQL / Paginación
**Definición:** Cláusula que salta un número determinado de filas del resultado antes de devolver los datos. Usada junto con LIMIT para implementar paginación por número de página (`OFFSET = (page - 1) * pageSize`). Tiene degradación de performance en páginas altas porque PostgreSQL debe recorrer y descartar todas las filas previas.
**En el proyecto:** La paginación simple del CRM usa OFFSET para las primeras versiones. Para producción con tablas grandes, se migra a cursor-based pagination que no tiene este problema de performance.

### OVER
**Categoría:** PostgreSQL / SQL / Window Functions
**Definición:** Cláusula que define la ventana (conjunto de filas) sobre la que opera una window function. Puede incluir PARTITION BY (divide en grupos), ORDER BY (define el orden dentro de cada grupo) y un frame (ROWS/RANGE BETWEEN). Sin PARTITION BY, la ventana es toda la tabla.
**En el proyecto:** `COUNT(*) OVER (PARTITION BY agent_id)` define una ventana de todas las filas del mismo agente, permitiendo mostrar el total de conversaciones del agente en cada fila sin perder el detalle individual.

---

## P

### PARTITION BY
**Categoría:** PostgreSQL / SQL / Window Functions
**Definición:** Sub-cláusula de OVER que divide el conjunto de filas en grupos independientes (particiones) según los valores de la columna indicada. La window function se calcula de forma independiente dentro de cada partición. Equivalente conceptual al GROUP BY pero sin colapsar filas.
**En el proyecto:** `PARTITION BY conversation_id` en la window function de último mensaje reinicia el ROW_NUMBER para cada conversación, permitiendo seleccionar exactamente el mensaje más reciente de cada una.

### Phantom Read
**Categoría:** Base de datos / Transacciones / Isolation Levels
**Definición:** Anomalía de concurrencia donde una transacción ejecuta la misma query dos veces y obtiene filas diferentes porque otra transacción insertó o eliminó filas que satisfacen la condición entre las dos ejecuciones. Prevenida solo en el nivel SERIALIZABLE.
**En el proyecto:** Si dos agentes ejecutan simultáneamente un SELECT de conversaciones sin asignar, ambos podrían ver las mismas conversaciones "disponibles". En el CRM se maneja con lógica de asignación atómica (UPDATE con WHERE) en lugar de escalar al nivel SERIALIZABLE.

### pg_stat_statements
**Categoría:** PostgreSQL / Performance / Monitoreo
**Definición:** Extensión oficial de PostgreSQL que registra estadísticas acumuladas de todas las queries ejecutadas: número de llamadas, tiempo total, tiempo promedio, filas procesadas. Permite identificar las queries más costosas del sistema en producción sin instrumentación adicional.
**En el proyecto:** Herramienta de diagnóstico de producción del CRM. El workflow es: `pg_stat_statements` identifica qué queries consumen más tiempo, `EXPLAIN ANALYZE` diagnostica la causa, y se crea el índice o se reescribe la query.

### pg_stat_user_tables
**Categoría:** PostgreSQL / Mantenimiento / Monitoreo
**Definición:** Vista del sistema que expone estadísticas por tabla: número de filas vivas (`n_live_tup`), número de dead tuples (`n_dead_tup`), fecha del último autovacuum y último autoanalyze. Permite monitorear la salud del mantenimiento de tablas.
**En el proyecto:** El DBA del CRM consulta esta vista para detectar tablas con acumulación excesiva de dead tuples y decidir si ejecutar VACUUM manualmente antes del próximo autovacuum.

### Query Planner
**Categoría:** PostgreSQL / Internals
**Definición:** Componente interno de PostgreSQL que analiza cada sentencia SQL y determina el plan de ejecución óptimo: qué índices usar, en qué orden hacer los JOINs, si usar Seq Scan o Index Scan. Sus decisiones se basan en las estadísticas de distribución de datos actualizadas por ANALYZE.
**En el proyecto:** El planner del CRM necesita estadísticas actualizadas (ANALYZE periódico) para elegir correctamente entre los múltiples índices disponibles en tablas como `conversations` y elegir el plan más rápido para las queries de la bandeja.

---

## Q

### QueryAsync
**Categoría:** Dapper / .NET
**Definición:** Método principal de Dapper que ejecuta un SELECT y mapea cada fila del resultado a un objeto del tipo genérico especificado, usando los alias de columna del SQL (`AS NombrePropiedad`) para hacer el mapping. Retorna `IEnumerable<T>` de forma asíncrona.
**En el proyecto:** Método principal del CRM para leer colecciones: listado paginado de conversaciones, búsqueda de contactos, historial de mensajes. El SQL explícito con alias garantiza que las propiedades del DTO se mapeen correctamente.

### QueryFirstAsync
**Categoría:** Dapper / .NET
**Definición:** Método de Dapper que retorna la primera fila del resultado sin importar cuántas haya, o null si no hay resultados. No lanza excepción si hay más de una fila. Útil cuando la query puede retornar varias filas pero solo se necesita una (con ORDER BY).
**En el proyecto:** Alternativa a `QuerySingleAsync` cuando se busca el mensaje más reciente de una conversación y puede haber coincidencias pero solo se necesita el primero según el ORDER BY.

### QueryMultipleAsync
**Categoría:** Dapper / .NET
**Definición:** Método de Dapper que ejecuta múltiples sentencias SQL en una sola llamada y retorna un `GridReader` que permite leer cada conjunto de resultados de forma independiente con `ReadAsync<T>()` y `ReadSingleAsync<T>()`. Reduce el número de round-trips a la base de datos.
**En el proyecto:** Obtiene datos paginados + total de registros del CRM en una sola llamada: primera sentencia retorna los ítems de la página, segunda retorna el COUNT total para el paginador del frontend.

### QuerySingleAsync (QuerySingleOrDefaultAsync)
**Categoría:** Dapper / .NET
**Definición:** Método de Dapper que retorna exactamente una fila o null (OrDefault) si no existe ninguna. Lanza excepción si hay más de una fila. Semánticamente correcto para búsquedas por clave primaria o identificador único donde se espera 0 o 1 resultado.
**En el proyecto:** Obtiene un contacto, conversación o agente por su `public_id` (GUID único). Si la query retorna más de una fila, es un error de datos que debe detectarse temprano.

---

## R

### RANK
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que asigna rangos según el ORDER BY definido en OVER, con huecos cuando hay empates. Si dos filas empatan en posición 1, la siguiente recibe posición 3 (saltando el 2). Útil para rankings competitivos donde la posición refleja exactamente cuántos competidores hay por delante.
**En el proyecto:** Rankea contactos por total de mensajes para el reporte de "clientes más activos" del CRM, con saltos en el ranking que reflejan correctamente los empates.

### READ COMMITTED
**Categoría:** PostgreSQL / Isolation Levels
**Definición:** Nivel de aislamiento por defecto de PostgreSQL. Cada sentencia SQL ve solo los datos que fueron commiteados antes de que esa sentencia comenzara. Previene dirty reads pero permite non-repeatable reads y phantom reads. Es el balance correcto entre seguridad y concurrencia para la mayoría de las aplicaciones.
**En el proyecto:** Nivel de aislamiento usado por defecto en el sistema de mensajería. Suficiente para la gestión de conversaciones, asignación de agentes y registro de mensajes sin necesidad de mayor aislamiento.

### READ UNCOMMITTED
**Categoría:** PostgreSQL / Isolation Levels
**Definición:** Nivel de aislamiento más bajo del estándar SQL que permitiría dirty reads. En PostgreSQL, este nivel se trata de forma idéntica a READ COMMITTED: el motor nunca permite dirty reads. Se menciona por completitud del estándar SQL pero no tiene efecto práctico en PostgreSQL.
**En el proyecto:** No tiene uso en el CRM. PostgreSQL garantiza que incluso si se especifica READ UNCOMMITTED, el comportamiento es el de READ COMMITTED.

### REPEATABLE READ
**Categoría:** PostgreSQL / Isolation Levels
**Definición:** Nivel de aislamiento donde todas las lecturas de una transacción ven el mismo snapshot de la base de datos tomado al inicio de la transacción. Previene dirty reads y non-repeatable reads. En PostgreSQL, también previene phantom reads (diferencia con el estándar SQL).
**En el proyecto:** No es necesario en el sistema de mensajería para las operaciones actuales. Se consideraría para operaciones de reportes donde múltiples queries deben ver el mismo estado consistente de los datos durante toda la generación del reporte.

### RIGHT JOIN
**Categoría:** SQL / PostgreSQL
**Definición:** Tipo de JOIN que devuelve todas las filas de la tabla derecha, con los valores de la tabla izquierda cuando hay coincidencia, o NULL cuando no la hay. Es el espejo de LEFT JOIN. En la práctica se evita: invertir el orden de las tablas y usar LEFT JOIN produce el mismo resultado con mayor claridad.
**En el proyecto:** No se usa en el CRM. Cualquier caso que requiriera RIGHT JOIN se reescribe invirtiendo el orden de las tablas y usando LEFT JOIN, que es la convención del equipo.

### ROLLBACK
**Categoría:** PostgreSQL / SQL / Transacciones
**Definición:** Comando que deshace todos los cambios realizados desde el BEGIN, devolviendo la base de datos al estado previo al inicio de la transacción. En .NET/Dapper se ejecuta en el bloque catch como `await tx.RollbackAsync()`.
**En el proyecto:** En el patrón Dapper del CRM, si el INSERT del mensaje de sistema falla después de un UPDATE de asignación exitoso, el ROLLBACK revierte el UPDATE también, garantizando atomicidad de la operación.

### ROWS BETWEEN ... AND ...
**Categoría:** PostgreSQL / Window Functions / Framing
**Definición:** Cláusula de framing de window functions que especifica exactamente qué filas incluir en la ventana de cálculo relativo a la fila actual. `ROWS UNBOUNDED PRECEDING` incluye desde el inicio. `ROWS BETWEEN 6 PRECEDING AND CURRENT ROW` incluye las 7 filas más recientes (incluyendo la actual).
**En el proyecto:** El CRM calcula promedios móviles de 7 días con `ROWS BETWEEN 6 PRECEDING AND CURRENT ROW` en la window function de `AVG(closed)` para mostrar tendencias de rendimiento de agentes en el dashboard.

### ROW_NUMBER
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que asigna un número entero único y secuencial a cada fila dentro de la partición según el ORDER BY, comenzando en 1. Sin huecos ni empates: aunque dos filas tengan el mismo valor de ordenamiento, reciben números diferentes. Es la window function más usada para paginación y selección del "top 1 por grupo".
**En el proyecto:** La query de bandeja del CRM usa `ROW_NUMBER() OVER (PARTITION BY conversation_id ORDER BY sent_at DESC) AS rn` para obtener el último mensaje de cada conversación filtrando `WHERE rn = 1`.

---

## S

### Scalar subquery
**Categoría:** SQL / PostgreSQL
**Definición:** Subquery ubicada en la lista de columnas del SELECT (no en FROM ni WHERE) que retorna exactamente un valor escalar por cada fila del outer query. Se re-ejecuta una vez por cada fila, lo que la hace potencialmente costosa en tablas grandes.
**En el proyecto:** Cuenta las conversaciones totales de cada contacto en el listado de contactos del CRM. Para grandes volúmenes conviene reemplazarla por un LEFT JOIN con GROUP BY o una window function.

### SELECT FOR UPDATE
**Categoría:** PostgreSQL / SQL / Transacciones
**Definición:** Sentencia que combina un SELECT con adquisición de locks exclusivos sobre las filas leídas. Permite leer el estado actual y luego modificarlo de forma segura, garantizando que ninguna otra transacción modifique esas filas hasta el COMMIT.
**En el proyecto:** Patrón de prevención de deadlocks en el CRM: adquirir locks sobre conversación y contacto en orden consistente con SELECT ... FOR UPDATE antes de ejecutar los UPDATE, evitando que dos requests compitan por los mismos recursos.

### SERIALIZABLE
**Categoría:** PostgreSQL / Isolation Levels
**Definición:** Nivel de aislamiento más estricto de PostgreSQL. Garantiza que el resultado de transacciones concurrentes es idéntico a si se hubieran ejecutado en serie, una tras otra. Previene todas las anomalías (dirty read, non-repeatable, phantom). Tiene mayor overhead y puede causar más rollbacks por conflictos serializables.
**En el proyecto:** No requerido para el sistema de mensajería en su versión actual. Solo sería necesario para operaciones financieras críticas como cargos de suscripción donde la correctitud exacta supera el costo de rendimiento.

### Sequential Scan (Seq Scan)
**Categoría:** PostgreSQL / Índices / EXPLAIN
**Definición:** Nodo de plan de ejecución donde PostgreSQL lee cada fila de la tabla de principio a fin para encontrar las que satisfacen la condición. Eficiente para tablas pequeñas o cuando se necesitan la mayoría de las filas. Muy lento para búsquedas selectivas en tablas grandes.
**En el proyecto:** En EXPLAIN ANALYZE, un Seq Scan sobre `conversations` o `messages` cuando se filtra por `agent_id` o `contact_id` es una señal de que falta el índice correspondiente.

### Soft delete
**Categoría:** Backend / Base de datos / Patrones
**Definición:** Patrón que marca registros como eliminados con un timestamp (`deleted_at`) en lugar de borrarlos físicamente. Permite recuperar datos, mantener historial de auditoría y simplificar foreign keys. El costo es que todas las queries deben incluir `WHERE deleted_at IS NULL`.
**En el proyecto:** Todas las entidades principales del CRM (contacts, conversations, agents) usan soft delete. El índice parcial `WHERE deleted_at IS NULL` minimiza el costo de performance de filtrar siempre por este campo.

### SQL Injection
**Categoría:** Seguridad / SQL
**Definición:** Vulnerabilidad donde un atacante inserta código SQL malicioso en la entrada de usuario que se concatena directamente en una query. Puede resultar en robo de datos, eliminación de tablas o escalación de privilegios. Se previene siempre usando parámetros nombrados (`@param`) nunca concatenación de strings.
**En el proyecto:** Regla absoluta del CRM: nunca concatenar o interpolar valores de usuario en SQL. Siempre usar parámetros `@nombre` con Dapper (objetos anónimos o DynamicParameters). El buscador de contactos usa `@name` y `@tag` como parámetros seguros.

### Stemming
**Categoría:** PostgreSQL / Full Text Search
**Definición:** Proceso de reducir una palabra a su raíz o lexema para que variantes morfológicas de la misma palabra sean tratadas como equivalentes en la búsqueda. Por ejemplo, "ayudando", "ayudó" y "ayuda" se reducen al lexema 'ayud' en español. PostgreSQL aplica stemming según el idioma configurado.
**En el proyecto:** Permite que la búsqueda FTS del CRM encuentre mensajes con "necesitaba" cuando el usuario busca "necesita", sin requerir lógica adicional en la aplicación.

### SUM OVER
**Categoría:** PostgreSQL / Window Functions
**Definición:** Window function que calcula la suma acumulativa (o por ventana) de una columna sin colapsar filas. Con `ROWS UNBOUNDED PRECEDING`, implementa una suma corriente de todos los valores desde el inicio hasta la fila actual, útil para gráficas de crecimiento acumulado.
**En el proyecto:** Calcula mensajes acumulados por día en los últimos 30 días para la gráfica de volumen del dashboard del CRM: cada fila del resultado tiene tanto el conteo diario como el total acumulado hasta ese día.

---

## T

### to_tsquery
**Categoría:** PostgreSQL / Full Text Search
**Definición:** Función que convierte un texto de búsqueda en un `tsquery` (patrón de consulta FTS), normalizando los términos según el idioma especificado y permitiendo operadores booleanos (`&` para AND, `|` para OR, `!` para NOT). Se combina con `to_tsvector` usando el operador `@@`.
**En el proyecto:** Construye los patrones de búsqueda del CRM: `to_tsquery('spanish', 'factura | pago')` encuentra mensajes que contengan "factura" o "pago" (y sus variantes morfológicas en español).

### to_tsvector
**Categoría:** PostgreSQL / Full Text Search
**Definición:** Función que convierte un texto en un `tsvector`, un documento normalizado y tokenizado donde cada palabra se reduce a su raíz (stemming) según el idioma y se registra con su posición. Es la representación interna que permite la búsqueda de texto completo eficiente.
**En el proyecto:** Normaliza el cuerpo de los mensajes del CRM para FTS: `to_tsvector('spanish', body)` convierte "necesito ayuda" en tokens como 'ayud' y 'necesit', permitiendo encontrar el mensaje con la búsqueda "ayuda" aunque el texto diga "ayudando".

### tsquery
**Categoría:** PostgreSQL / Full Text Search / Tipos de dato
**Definición:** Tipo de dato que representa un patrón de búsqueda de texto completo con operadores booleanos. Contiene los términos de búsqueda normalizados y sus relaciones lógicas (AND, OR, NOT). Se usa con el operador `@@` para comparar contra un `tsvector`.
**En el proyecto:** El resultado de `to_tsquery('spanish', 'pago & urgente')` es un tsquery que el operador `@@` compara contra el tsvector del cuerpo de los mensajes del CRM para encontrar mensajes sobre pagos urgentes.

### tsvector
**Categoría:** PostgreSQL / Full Text Search / Tipos de dato
**Definición:** Tipo de dato que representa un documento indexado para Full Text Search. Contiene los lexemas (raíces) de las palabras del texto con sus posiciones de aparición. Puede almacenarse en una columna generada o calcularse en tiempo de query con `to_tsvector()`.
**En el proyecto:** Generado dinámicamente en la query del CRM con `to_tsvector('spanish', body)`. Para máximo rendimiento en producción se puede agregar una columna generada `body_tsv tsvector GENERATED ALWAYS AS (to_tsvector('spanish', body)) STORED`.

---

## V

### VACUUM
**Categoría:** PostgreSQL / Mantenimiento
**Definición:** Operación de mantenimiento que recupera el espacio ocupado por dead tuples, actualiza los mapas de visibilidad y libera páginas para reutilización. No requiere lock exclusivo sobre la tabla (a diferencia de VACUUM FULL). Es ejecutado automáticamente por autovacuum o manualmente.
**En el proyecto:** Mantiene la eficiencia de las tablas de alta escritura del CRM (`messages`, `conversations`). Se ejecuta manualmente con `VACUUM ANALYZE messages` tras importaciones masivas de datos o migraciones.

### VACUUM ANALYZE
**Categoría:** PostgreSQL / Mantenimiento
**Definición:** Combinación que ejecuta VACUUM (limpia dead tuples) y ANALYZE (actualiza estadísticas) en una sola operación. Es el comando de mantenimiento más completo para una tabla y el recomendado después de operaciones masivas de INSERT, UPDATE o DELETE.
**En el proyecto:** El comando post-importación del CRM: tras cargar miles de contactos desde CSV, `VACUUM ANALYZE contacts` asegura espacio recuperado y estadísticas actualizadas para que el planner genere planes óptimos de inmediato.

### VACUUM FULL
**Categoría:** PostgreSQL / Mantenimiento
**Definición:** Variante de VACUUM que reconstruye completamente la tabla, recuperando todo el espacio en disco incluyendo el que el VACUUM estándar no puede liberar. Requiere un lock exclusivo sobre la tabla (bloquea todas las lecturas y escrituras) y puede tardar mucho en tablas grandes. Solo usar en ventanas de mantenimiento.
**En el proyecto:** No se recomienda en producción del CRM sin una ventana de mantenimiento planificada. El autovacuum estándar debe ser suficiente para las tablas del CRM con la configuración correcta.

---

## W

### WAL (Write-Ahead Log)
**Categoría:** PostgreSQL / Durabilidad / Internals
**Definición:** Archivo de log donde PostgreSQL registra todos los cambios antes de aplicarlos a las páginas de datos. Garantiza la propiedad de Durabilidad de ACID: si el servidor cae antes de escribir en las páginas de datos, el WAL permite reconstruir los cambios al reiniciar. También es la base de la replicación streaming.
**En el proyecto:** Garantía de que ningún mensaje del CRM commiteado se pierde por un reinicio del servidor PostgreSQL. También permite configurar réplicas de lectura para escalar el CRM sin duplicar la carga en el servidor primario.

### WHERE (cláusula de filtro)
**Categoría:** SQL / PostgreSQL
**Definición:** Cláusula SQL que filtra las filas del resultado según una condición booleana. Es la cláusula más crítica para el rendimiento: las columnas que aparecen frecuentemente en WHERE son candidatas a índices. El orden de evaluación de las condiciones puede afectar qué índice usa el planner.
**En el proyecto:** Todas las queries del CRM incluyen `WHERE deleted_at IS NULL` como condición base de soft delete, además de los filtros específicos de cada operación (por `agent_id`, `status`, `contact_id`).

### Window Function
**Categoría:** PostgreSQL / SQL
**Definición:** Función SQL que calcula un valor por cada fila usando un conjunto de filas relacionadas (la "ventana") sin colapsar el resultado. A diferencia de GROUP BY, preserva todas las filas originales. Se define con la cláusula OVER. Incluye funciones de ranking (ROW_NUMBER, RANK, DENSE_RANK), navegación (LAG, LEAD) y agregación (SUM, AVG, COUNT OVER).
**En el proyecto:** El CRM usa window functions para: obtener el último mensaje de cada conversación (ROW_NUMBER), calcular el ranking de agentes por carga de trabajo (ROW_NUMBER OVER ORDER BY) y métricas acumuladas de mensajes por día (SUM OVER).

### WITH (CTE)
**Categoría:** SQL / PostgreSQL
**Definición:** Palabra clave que introduce una o más Common Table Expressions antes del SELECT principal. Mejora la legibilidad al dar nombres descriptivos a resultados intermedios complejos. En PostgreSQL, las CTEs son optimizadas como cualquier otro subquery desde la versión 12.
**En el proyecto:** Todas las queries complejas del CRM (bandeja, reportes, rankings) usan WITH para estructurar los pasos: `WITH active_contacts AS (...), last_message AS (...), open_conversations AS (...)`.

---

*Raptor Dev Services  -  Dev Notes 2026*
