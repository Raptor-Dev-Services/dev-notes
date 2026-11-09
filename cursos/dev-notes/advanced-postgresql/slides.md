<!-- _class: lead -->

# PostgreSQL Avanzado

## Consultas  -  Índices  -  Transacciones  -  JSONB  -  Window Functions

**Dev Notes  -  Raptor Dev Services**
*Material de auto-estudio  -  Sistema de mensajería*

---

<!-- _class: lead -->

# Parte 1

## Consultas Avanzadas
### JOINs  -  Subqueries  -  CTEs

---

## JOINs  -  regla de decisión

| Tipo | Cuándo usar | Resultado |
|------|-------------|-----------|
| **INNER JOIN** | Solo filas que hacen match en ambas tablas | Conversaciones con agente asignado |
| **LEFT JOIN** | Todas las filas de la izquierda, con o sin match | Todos los contactos, tengan o no conversaciones |
| **RIGHT JOIN** | Todas las filas de la derecha (raro  -  usar LEFT) | Evitar; invertir el orden y usar LEFT |
| **CROSS JOIN** | Producto cartesiano de ambas tablas | Combinaciones de canales × agentes |

```
¿Quiero todas las filas aunque no tengan relación? -> LEFT JOIN
¿Solo filas que tienen relación?                   -> INNER JOIN
```

---

## INNER JOIN vs LEFT JOIN en el CRM

```sql
-- INNER JOIN  -  solo conversaciones con agente asignado
SELECT c.id, c.status, a.name AS agent_name
FROM   conversations c
INNER JOIN agents a ON a.id = c.agent_id
WHERE  c.deleted_at IS NULL;

-- LEFT JOIN  -  todas las conversaciones, asignadas o no
SELECT c.id, c.status, a.name AS agent_name
FROM   conversations c
LEFT JOIN agents a ON a.id = c.agent_id
WHERE  c.deleted_at IS NULL;
-- agent_name = NULL para conversaciones sin agente
```

---

## Subqueries  -  tipos principales

```sql
-- En WHERE (scalar subquery)
SELECT * FROM contacts
WHERE id IN (
    SELECT contact_id FROM conversations
    WHERE status = 'open'
);

-- Correlacionada  -  se ejecuta por cada fila del outer query
SELECT c.name,
       (SELECT COUNT(*) FROM conversations cv
        WHERE cv.contact_id = c.id) AS total_conversations
FROM   contacts c
WHERE  c.deleted_at IS NULL;

-- En FROM (derived table)
SELECT agent_id, total
FROM (
    SELECT agent_id, COUNT(*) AS total
    FROM   conversations
    WHERE  created_at > NOW() - INTERVAL '30 days'
    GROUP BY agent_id
) AS recent_stats
ORDER BY total DESC;
```

---

## CTEs  -  Common Table Expressions

Las CTEs (WITH) nombran un resultado intermedio para reutilizarlo. Mejoran la legibilidad vs subqueries anidados.

```sql
--  Sin CTE  -  anidado y difícil de leer
SELECT a.name, stats.total
FROM agents a
INNER JOIN (
    SELECT agent_id, COUNT(*) AS total
    FROM   conversations
    WHERE  status = 'open' AND deleted_at IS NULL
    GROUP BY agent_id
) AS stats ON stats.agent_id = a.id;

-- [OK] Con CTE  -  legible y componible
WITH open_by_agent AS (
    SELECT agent_id, COUNT(*) AS total
    FROM   conversations
    WHERE  status = 'open' AND deleted_at IS NULL
    GROUP BY agent_id
)
SELECT a.name, oba.total
FROM   agents a
INNER JOIN open_by_agent oba ON oba.agent_id = a.id
ORDER BY oba.total DESC;
```

---

## CTEs encadenadas  -  bandeja de entrada del CRM

```sql
WITH
active_contacts AS (
    SELECT id, name, email
    FROM   contacts
    WHERE  deleted_at IS NULL
),
last_message AS (
    SELECT DISTINCT ON (conversation_id)
           conversation_id, body, sent_at
    FROM   messages
    ORDER BY conversation_id, sent_at DESC
),
open_conversations AS (
    SELECT c.id, c.contact_id, c.agent_id, c.status,
           lm.body  AS last_message,
           lm.sent_at AS last_activity
    FROM   conversations c
    LEFT JOIN last_message lm ON lm.conversation_id = c.id
    WHERE  c.status = 'open' AND c.deleted_at IS NULL
)
SELECT oc.id, ac.name, ac.email, oc.last_message, oc.last_activity
FROM   open_conversations oc
JOIN   active_contacts ac ON ac.id = oc.contact_id
ORDER BY oc.last_activity DESC NULLS LAST;
```

---

<!-- _class: lead -->

# Parte 2

## Índices
### B-Tree  -  GIN  -  GiST  -  Parciales  -  EXPLAIN ANALYZE

---

## El problema sin índices

```sql
-- Sin índice en email  -  PostgreSQL lee TODAS las filas
SELECT * FROM contacts WHERE email = 'ana@example.com';
-- En 10,000,000 filas: varios segundos

-- Con índice B-tree  -  navega el árbol directamente
-- En cualquier tamaño: milisegundos
```

**Sequential scan vs Index scan:**

| | Seq Scan | Index Scan |
|---|---|---|
| 1,000 filas | Rápido | Rápido |
| 1,000,000 filas | Lento | Rápido |
| Costo de escritura | Ninguno | Actualiza el índice |

> Regla: crear índice cuando la tabla supera ~10,000 filas y la columna aparece frecuentemente en WHERE.

---

## Tipos de índice

| Tipo | Cuándo usar | Ejemplo CRM |
|------|-------------|-------------|
| **B-tree** (default) | Igualdad, rangos, ORDER BY, LIKE prefijo | `email`, `created_at`, `status` |
| **GIN** | Arrays, JSONB, Full Text Search | `metadata JSONB`, FTS en `body` |
| **GiST** | Datos geoespaciales, rangos solapados | Coordenadas de contacto |
| **HASH** | Solo igualdad exacta (raramente mejor que B-tree) | Evitar |
| **BRIN** | Tablas muy grandes con datos correlacionados | Logs de auditoría por fecha |

```sql
-- GIN para JSONB
CREATE INDEX IF NOT EXISTS ix_contacts_metadata
    ON contacts USING GIN (metadata);

-- GIN para Full Text Search
CREATE INDEX IF NOT EXISTS ix_messages_body_fts
    ON messages USING GIN (to_tsvector('spanish', body));
```

---

## Índices parciales  -  solo filas relevantes

```sql
-- Solo conversaciones abiertas  -  el 95% de queries filtra status = 'open'
CREATE INDEX IF NOT EXISTS ix_conversations_open_agent
    ON conversations (agent_id, created_at DESC)
    WHERE status = 'open' AND deleted_at IS NULL;

-- Solo contactos activos
CREATE INDEX IF NOT EXISTS ix_contacts_email_active
    ON contacts (email)
    WHERE deleted_at IS NULL;
```

**Ventaja:** el índice parcial es mucho más pequeño -> cabe en caché -> más rápido.

```sql
-- [OK] PostgreSQL USA el índice parcial
SELECT * FROM contacts WHERE email = 'ana@example.com' AND deleted_at IS NULL;

-- [Err] PostgreSQL NO usa el índice parcial (falta la condición del WHERE)
SELECT * FROM contacts WHERE email = 'ana@example.com';
```

---

## Índices compuestos  -  orden importa

```sql
-- Índice en (agent_id, created_at)
CREATE INDEX IF NOT EXISTS ix_conversations_agent_created
    ON conversations (agent_id, created_at DESC)
    WHERE deleted_at IS NULL;

-- [OK] Usa el índice  -  coincide con el orden de columnas
SELECT * FROM conversations
WHERE agent_id = @agentId AND deleted_at IS NULL
ORDER BY created_at DESC;

-- [OK] También usa el índice (solo primer campo)
SELECT * FROM conversations WHERE agent_id = @agentId;

-- [Err] No usa el índice (solo segundo campo sin el primero)
SELECT * FROM conversations WHERE created_at > NOW() - INTERVAL '1 day';
```

> Regla: la primera columna del índice compuesto debe ser la de mayor selectividad.

---

## EXPLAIN ANALYZE  -  leer el plan de ejecución

```sql
-- Ver el plan Y ejecutar con tiempos reales
EXPLAIN ANALYZE
SELECT c.id, ct.name, a.name AS agent
FROM   conversations c
JOIN   contacts ct ON ct.id = c.contact_id
JOIN   agents a    ON a.id  = c.agent_id
WHERE  c.status = 'open' AND c.deleted_at IS NULL
ORDER BY c.created_at DESC
LIMIT 50;
```

**Nodos clave:**

| Nodo | Significado | ¿Problema? |
|------|-------------|-----------|
| `Index Scan` | Usa el índice | No |
| `Index Only Scan` | Solo lee el índice | Muy eficiente |
| `Seq Scan` | Lee toda la tabla | Sí, si la tabla es grande |
| `Nested Loop` | Join fila por fila | Caro con muchas filas |

`cost=inicio..total`  -  unidades relativas internas, no milisegundos. Lo importante es la magnitud.

---

## Cuándo crear (y cuándo NO) un índice

```
[OK] Columnas frecuentes en WHERE
[OK] Foreign keys (siempre)
[OK] Columnas en ORDER BY de queries paginados
[OK] Columnas UNIQUE
[OK] Usar índice parcial si la mayoría filtra deleted_at IS NULL

[Err] Tablas con < 1,000 filas (seq scan puede ser igual de rápido)
[Err] Columnas booleanas en tablas de 10M filas (baja cardinalidad)
[Err] Demasiados índices en tablas con muchos INSERT  -  cada índice
  ralentiza escrituras
```

---

<!-- _class: lead -->

# Parte 3

## Transacciones
### ACID  -  Isolation Levels  -  Deadlocks

---

## ACID  -  las 4 propiedades

| Propiedad | Garantía | Ejemplo CRM |
|-----------|----------|-------------|
| **Atomicidad** | Todo o nada  -  si una operación falla, todo se revierte | Asignar conversación + notificar agente |
| **Consistencia** | La DB siempre pasa de un estado válido a otro | Un mensaje no puede existir sin su conversación |
| **Aislamiento** | Las transacciones concurrentes no se ven entre sí | Dos agentes tomando la misma conversación |
| **Durabilidad** | Los datos commiteados sobreviven a reinicios | Un mensaje enviado no desaparece |

```sql
BEGIN;
  UPDATE conversations SET agent_id = @agentId, status = 'assigned' WHERE id = @convId;
  INSERT INTO messages (conversation_id, body, sent_at) VALUES (@convId, 'Asignado', NOW());
COMMIT;
-- Si falla cualquier operación -> ROLLBACK automático
```

---

## Isolation Levels  -  cuánto se aíslan las transacciones

| Nivel | Dirty Read | Non-repeatable | Phantom |
|-------|-----------|----------------|---------|
| `READ UNCOMMITTED` | Posible | Posible | Posible |
| **`READ COMMITTED`** (default PostgreSQL) | No | Posible | Posible |
| `REPEATABLE READ` | No | No | Posible |
| `SERIALIZABLE` | No | No | No |

**Dirty Read:** lees datos de una transacción que luego hace ROLLBACK  -  nunca existieron.
**Non-repeatable:** la misma fila da resultados distintos en dos lecturas de la misma transacción.
**Phantom:** la misma query devuelve filas diferentes (alguien insertó/eliminó entre lecturas).

> Para el CRM: `READ COMMITTED` (default) es suficiente. Solo usa `SERIALIZABLE` en operaciones financieras críticas.

---

## Transacciones en Dapper  -  patrón del proyecto

```csharp
public async Task AssignConversationAsync(
    Guid conversationId, Guid agentId, CancellationToken ct)
{
    await using var conn = await _factory.OpenConnectionAsync(ct);
    await using var tx   = await conn.BeginTransactionAsync(ct);
    try
    {
        await conn.ExecuteAsync(
            """UPDATE conversations
               SET agent_id = @agentId, status = 'assigned',
                   updated_at = timezone('utc', now())
               WHERE id = @conversationId AND deleted_at IS NULL;""",
            new { agentId, conversationId }, transaction: tx);

        await conn.ExecuteAsync(
            """INSERT INTO messages (conversation_id, body, type, sent_at)
               VALUES (@conversationId, 'Conversación asignada', 'system',
                       timezone('utc', now()));""",
            new { conversationId }, transaction: tx);

        await tx.CommitAsync(ct);
    }
    catch { await tx.RollbackAsync(ct); throw; }
}
```

---

## Deadlocks  -  causa y prevención

```
T1: LOCK conversations(id=5) -> espera contacts(id=10)
T2: LOCK contacts(id=10)     -> espera conversations(id=5)
-> Ambas esperan para siempre -> PostgreSQL mata la más barata de reintentar
```

**Prevención  -  ordenar locks siempre igual:**

```sql
-- [Err] T1 y T2 adquieren locks en orden diferente -> deadlock posible
-- T1: UPDATE conversations ... ; UPDATE contacts ...
-- T2: UPDATE contacts ...       ; UPDATE conversations ...

-- [OK] Siempre el mismo orden (por ejemplo, por tabla y luego por Id menor)
BEGIN;
SELECT id FROM conversations WHERE id = @convId FOR UPDATE;
SELECT id FROM contacts       WHERE id = @contactId FOR UPDATE;
-- Ahora hacer los UPDATE  -  los locks ya están adquiridos en orden
COMMIT;
```

> Regla: mantener las transacciones cortas. Menos tiempo con locks = menos probabilidad de deadlock.

---

<!-- _class: lead -->

# Parte 4

## PostgreSQL Features Avanzados
### JSONB  -  Full Text Search  -  ARRAY types

---

## JSONB  -  datos semi-estructurados

```sql
-- Agregar columna JSONB a contacts (metadata flexible por canal)
ALTER TABLE contacts ADD COLUMN IF NOT EXISTS metadata JSONB;

-- Insertar / actualizar JSON
UPDATE contacts
SET metadata = '{"whatsapp_id": "521234567890", "instagram_handle": "@usuario",
                 "tags": ["vip", "recurrente"], "score": 92}'
WHERE public_id = @publicId;
```

**Operadores JSONB:**

```sql
-- ->  retorna JSON (con comillas)
SELECT metadata->'whatsapp_id'  FROM contacts;   -- "521234567890"

-- ->> retorna texto
SELECT metadata->>'whatsapp_id' FROM contacts;   -- 521234567890

-- #>> ruta anidada
SELECT metadata#>>'{address,city}' FROM contacts;

-- @> contiene subconjunto
SELECT * FROM contacts WHERE metadata @> '{"tags": ["vip"]}';

-- ? tiene la clave
SELECT * FROM contacts WHERE metadata ? 'instagram_handle';
```

---

## JSONB  -  actualizar sin reescribir todo

```sql
-- Actualizar un solo campo con ||  (merge / concatenar)
UPDATE contacts
SET metadata = metadata || '{"score": 95}'
WHERE public_id = @publicId;

-- Eliminar una clave con operador #-
UPDATE contacts
SET metadata = metadata #- '{instagram_handle}'
WHERE public_id = @publicId;

-- Indexar JSONB con GIN para búsquedas rápidas
CREATE INDEX IF NOT EXISTS ix_contacts_metadata_gin
    ON contacts USING GIN (metadata);

-- El índice acelera el operador @>
EXPLAIN ANALYZE
SELECT id, name FROM contacts WHERE metadata @> '{"tags": ["vip"]}';
-- -> Bitmap Index Scan using ix_contacts_metadata_gin
```

---

## Full Text Search (FTS)  -  búsqueda en mensajes

```sql
-- tsvector: documento tokenizado y normalizado
SELECT to_tsvector('spanish', 'Hola, necesito ayuda con mi pedido urgente');
-- 'ayud':4 'hola':1 'necesit':2 'pedir':6 'urgent':5

-- tsquery: patrón de búsqueda
SELECT to_tsquery('spanish', 'ayuda & pedido');

-- Buscar mensajes que contengan "factura" o "pago"
SELECT id, body, sent_at
FROM   messages
WHERE  to_tsvector('spanish', body) @@ to_tsquery('spanish', 'factura | pago')
ORDER BY sent_at DESC;

-- Índice GIN para FTS  -  sin él, cada búsqueda es un seq scan
CREATE INDEX IF NOT EXISTS ix_messages_body_fts
    ON messages USING GIN (to_tsvector('spanish', body));
```

---

## ARRAY types  -  etiquetas en contacts

```sql
-- Columna de tipo array
ALTER TABLE contacts ADD COLUMN tags TEXT[] DEFAULT '{}';

-- Insertar / actualizar tags
UPDATE contacts
SET tags = ARRAY['vip', 'recurrente', 'mexico']
WHERE id = @id;

-- Agregar un tag sin reemplazar los existentes
UPDATE contacts
SET tags = array_append(tags, 'nuevo_tag')
WHERE id = @id;

-- Filtrar contactos con tag específico
SELECT id, name FROM contacts
WHERE 'vip' = ANY(tags) AND deleted_at IS NULL;

-- Índice GIN para búsquedas en arrays
CREATE INDEX IF NOT EXISTS ix_contacts_tags_gin
    ON contacts USING GIN (tags);
```

---

<!-- _class: lead -->

# Parte 5

## Window Functions
### ROW_NUMBER  -  RANK  -  LAG/LEAD  -  SUM/AVG OVER

---

## Window Functions  -  qué son y qué resuelven

A diferencia de `GROUP BY`, las window functions calculan un valor **por cada fila** usando un conjunto de filas relacionadas  -  sin colapsar el resultado.

```sql
-- GROUP BY: colapsa filas -> pierdes el detalle
SELECT agent_id, COUNT(*) AS total
FROM   conversations
GROUP BY agent_id;

-- Window function: mantiene todas las filas + agrega el cálculo
SELECT id, agent_id, status,
       COUNT(*) OVER (PARTITION BY agent_id) AS agent_total
FROM   conversations
WHERE  deleted_at IS NULL;
-- Cada fila conserva su id y status, pero también tiene el total del agente
```

---

## ROW_NUMBER y RANK

```sql
-- ROW_NUMBER  -  número continuo sin huecos ni empates
SELECT agent_id, name,
       COUNT(c.id) AS open_conversations,
       ROW_NUMBER() OVER (ORDER BY COUNT(c.id) DESC) AS position
FROM   agents a
LEFT JOIN conversations c ON c.agent_id = a.id AND c.status = 'open'
GROUP BY a.id, a.name
ORDER BY position;

-- RANK  -  igual pero con huecos en empates (1,1,3...)
-- DENSE_RANK  -  igual pero sin huecos (1,1,2...)
SELECT contact_id,
       RANK()       OVER (ORDER BY total_messages DESC) AS rank_with_gaps,
       DENSE_RANK() OVER (ORDER BY total_messages DESC) AS rank_no_gaps
FROM (
    SELECT contact_id, COUNT(*) AS total_messages FROM messages GROUP BY contact_id
) AS msg_stats;
```

---

## PARTITION BY  -  reiniciar el cálculo por grupo

```sql
-- Obtener el último mensaje de CADA conversación (caso real de bandeja)
WITH ranked AS (
    SELECT conversation_id, body, sent_at,
           ROW_NUMBER() OVER (
               PARTITION BY conversation_id   -- reinicia por conversación
               ORDER BY sent_at DESC          -- el más reciente = ROW_NUMBER 1
           ) AS rn
    FROM messages
)
SELECT conversation_id, body, sent_at
FROM   ranked
WHERE  rn = 1;
-- Equivalente a DISTINCT ON (conversation_id) ORDER BY sent_at DESC
```

---

## LAG / LEAD  -  filas anterior / siguiente

```sql
-- Tiempo transcurrido entre mensajes consecutivos en una conversación
SELECT id, sent_at, body,
       LAG(sent_at)  OVER (PARTITION BY conversation_id ORDER BY sent_at) AS prev_sent,
       LEAD(sent_at) OVER (PARTITION BY conversation_id ORDER BY sent_at) AS next_sent,
       sent_at - LAG(sent_at) OVER (
           PARTITION BY conversation_id ORDER BY sent_at
       ) AS time_since_prev
FROM messages
WHERE conversation_id = @convId
ORDER BY sent_at;
-- time_since_prev = NULL para el primer mensaje (no hay LAG)
-- Útil para calcular tiempos de respuesta de agentes
```

---

## SUM / AVG acumulativo  -  métricas del CRM

```sql
-- Mensajes acumulados por día (para gráfica de volumen)
SELECT date_trunc('day', sent_at) AS day,
       COUNT(*) AS daily_messages,
       SUM(COUNT(*)) OVER (
           ORDER BY date_trunc('day', sent_at)
           ROWS UNBOUNDED PRECEDING
       ) AS cumulative_messages
FROM   messages
WHERE  sent_at >= NOW() - INTERVAL '30 days'
GROUP BY day
ORDER BY day;

-- AVG móvil de conversaciones cerradas por agente (últimos 7 días)
SELECT agent_id, day, closed,
       AVG(closed) OVER (
           PARTITION BY agent_id
           ORDER BY day
           ROWS BETWEEN 6 PRECEDING AND CURRENT ROW
       ) AS rolling_7d_avg
FROM   daily_agent_stats;
```

---

<!-- _class: lead -->

# Parte 6

## Dapper con PostgreSQL
### QueryAsync  -  QuerySingleOrDefaultAsync  -  Multi-mapping  -  DynamicParameters

---

## Métodos principales de MainDapperDbConnection

```csharp
// Múltiples filas
IEnumerable<T> results = await _db.QueryAsync<T>(sql, @params, ct);

// 0 o 1 fila  -  retorna null si no existe (usar para GetById)
T? result = await _db.QuerySingleAsync<T>(sql, @params, ct);

// Primera fila o null (no lanza si hay más de 1 fila)
T? result = await _db.QueryFirstAsync<T>(sql, @params, ct);

// INSERT / UPDATE / DELETE  -  retorna filas afectadas
int affected = await _db.ExecuteAsync(sql, @params, ct);

// COUNT / EXISTS / valor escalar
int count   = await _db.ExecuteScalarAsync<int>(sql, ct: ct);
bool exists = await _db.ExecuteScalarAsync<bool>(sql, @params, ct);
```

**Regla absoluta:** nunca concatenar o interpolar valores del usuario en SQL  -  siempre parámetros `@nombre`.

---

## QueryAsync con CRM  -  listado paginado de conversaciones

```csharp
public Task<IEnumerable<ConversationSummaryDto>> GetPagedAsync(
    Guid agentId, int page, int pageSize, CancellationToken ct) =>
    _db.QueryAsync<ConversationSummaryDto>(
        """
        SELECT c.public_id   AS PublicId,
               c.status      AS Status,
               ct.name       AS ContactName,
               m.body        AS LastMessage,
               m.sent_at     AS LastActivity
        FROM   conversations c
        JOIN   contacts ct  ON ct.id = c.contact_id
        LEFT JOIN LATERAL (
            SELECT body, sent_at FROM messages
            WHERE  conversation_id = c.id
            ORDER BY sent_at DESC LIMIT 1
        ) m ON true
        WHERE  c.agent_id   = @agentId
          AND  c.deleted_at IS NULL
        ORDER BY m.sent_at DESC NULLS LAST
        LIMIT  @pageSize OFFSET @offset;
        """,
        new { agentId, pageSize, offset = (page - 1) * pageSize },
        ct);
```

---

## Multi-resultado  -  datos + total en una sola ida a la DB

```csharp
public async Task<(IEnumerable<ConversationSummaryDto> Items, int Total)>
    GetPagedWithCountAsync(Guid agentId, int page, int pageSize, CancellationToken ct)
{
    await using var conn = await _factory.OpenConnectionAsync(ct);

    const string sql = """
        SELECT public_id AS PublicId, status AS Status, created_at AS CreatedAt
        FROM   conversations
        WHERE  agent_id = @agentId AND deleted_at IS NULL
        ORDER BY created_at DESC
        LIMIT  @pageSize OFFSET @offset;

        SELECT COUNT(*) FROM conversations
        WHERE  agent_id = @agentId AND deleted_at IS NULL;
        """;

    using var multi = await conn.QueryMultipleAsync(sql,
        new { agentId, pageSize, offset = (page - 1) * pageSize });

    var items = await multi.ReadAsync<ConversationSummaryDto>();
    var total = await multi.ReadSingleAsync<int>();
    return (items, total);
}
```

---

## Parámetros seguros  -  DynamicParameters para casos dinámicos

```csharp
// Filtros opcionales  -  construir WHERE dinámico sin concatenar strings
public Task<IEnumerable<ContactDto>> SearchAsync(
    string? name, string? tag, bool? hasOpenConversation, CancellationToken ct)
{
    var sql    = new StringBuilder(
        "SELECT id AS Id, name AS Name, email AS Email FROM contacts WHERE deleted_at IS NULL");
    var @params = new DynamicParameters();

    if (!string.IsNullOrEmpty(name))
    { sql.Append(" AND name ILIKE @name"); @params.Add("name", $"%{name}%"); }

    if (!string.IsNullOrEmpty(tag))
    { sql.Append(" AND @tag = ANY(tags)"); @params.Add("tag", tag); }

    sql.Append(" ORDER BY name LIMIT 100;");

    return _db.QueryAsync<ContactDto>(sql.ToString(), @params, ct);
}
```

---

<!-- _class: lead -->

# Parte 7

## Performance
### Connection Pooling  -  VACUUM/ANALYZE  -  pg_stat_statements

---

## Connection Pooling  -  Npgsql

```csharp
// [OK] Npgsql gestiona el pool automáticamente con la connection string
// No crear NpgsqlConnection directamente en cada request

// appsettings.json
"ConnectionStrings": {
  "MainDb": "Host=localhost;Database=mensajeria;Username=app;
             Password=secret;Minimum Pool Size=5;Maximum Pool Size=100;
             Connection Idle Lifetime=300;Connection Pruning Interval=10"
}
```

**Parámetros importantes:**

| Parámetro | Default | Recomendado CRM |
|-----------|---------|-----------------|
| `Maximum Pool Size` | 100 | 50-100 por instancia |
| `Minimum Pool Size` | 0 | 5 (evitar cold start) |
| `Connection Idle Lifetime` | 300s | 300s |
| `Command Timeout` | 30s | 30s (ajustar según query) |

> Nunca usar `NpgsqlConnection` con `new` fuera del factory  -  rompe el pool.

---

## VACUUM y ANALYZE  -  mantenimiento de tablas

PostgreSQL usa MVCC: los UPDATE y DELETE no borran filas inmediatamente  -  crean "dead tuples" que acumulan espacio.

```sql
-- Ver estado de tablas con dead tuples acumuladas
SELECT schemaname, tablename,
       n_live_tup, n_dead_tup,
       last_autovacuum, last_autoanalyze
FROM   pg_stat_user_tables
ORDER BY n_dead_tup DESC
LIMIT 10;

-- VACUUM  -  limpia dead tuples (no requiere lock exclusivo)
VACUUM conversations;

-- ANALYZE  -  actualiza estadísticas para el query planner
ANALYZE conversations;

-- VACUUM ANALYZE  -  ambos a la vez (tras INSERT/UPDATE masivo)
VACUUM ANALYZE messages;
```

> `autovacuum` se ejecuta en segundo plano automáticamente. Solo intervenir si `n_dead_tup` es muy alto.

---

## pg_stat_statements  -  identificar queries lentos

```sql
-- Extensión que registra estadísticas de todas las queries ejecutadas
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Top 10 queries más lentos por tiempo total
SELECT query, calls,
       round(total_exec_time::numeric, 2) AS total_ms,
       round(mean_exec_time::numeric, 2)  AS avg_ms,
       rows
FROM   pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;

-- Queries con más llamadas (candidatos para caching)
SELECT query, calls, round(mean_exec_time::numeric, 2) AS avg_ms
FROM   pg_stat_statements
ORDER BY calls DESC
LIMIT 10;
```

> Workflow: `pg_stat_statements` identifica el problema -> `EXPLAIN ANALYZE` diagnostica -> crear índice o reescribir la query.

---

## Resumen  -  decisiones del sistema de mensajería

| Escenario | Solución |
|-----------|----------|
| Bandeja de entrada paginada | CTE + `LEFT JOIN LATERAL` + índice compuesto en `(agent_id, created_at DESC)` |
| Búsqueda de contactos por nombre | FTS con `to_tsvector` + índice GIN |
| Metadata de canal (WhatsApp/Instagram) | Columna `JSONB` + índice GIN + operador `@>` |
| Ranking de agentes por conversaciones | Window function `ROW_NUMBER() OVER (ORDER BY ...)` |
| Asignación de conversación (atómica) | Transacción Dapper con `BeginTransactionAsync` |
| Diagnóstico de query lento | `EXPLAIN ANALYZE` + `pg_stat_statements` |
| Soft delete sin leer filas eliminadas | Índice parcial `WHERE deleted_at IS NULL` |

---

<!-- _class: lead -->

# Fin del módulo

## PostgreSQL Avanzado

**Consultas  -  Índices  -  Transacciones  -  JSONB  -  Window Functions**

*Raptor Dev Services  -  Dev Notes 2026*
