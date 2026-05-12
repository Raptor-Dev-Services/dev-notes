# 04 — PostgreSQL Avanzado: CTEs, Window Functions, JSONB y ON CONFLICT

Herramientas de SQL que van más allá de SELECT/INSERT/UPDATE básicos.

---

## CTEs — Common Table Expressions

Una CTE (WITH clause) nombra un resultado intermedio y permite reusarlo en la misma query. Mejora la legibilidad de queries complejas.

```sql
-- ❌ Sin CTE — subquery anidado difícil de leer
SELECT u.FullName, orders.OrderCount
FROM dbo.ExampleUsers u
INNER JOIN (
    SELECT UserId, COUNT(*) AS OrderCount
    FROM dbo.ExampleOrders
    WHERE CreatedAtUtc > NOW() - INTERVAL '30 days'
    GROUP BY UserId
) AS orders ON orders.UserId = u.Id
WHERE u.IsActive = true;

-- ✓ Con CTE — legible y componible
WITH recent_orders AS (
    SELECT UserId, COUNT(*) AS OrderCount
    FROM   dbo.ExampleOrders
    WHERE  CreatedAtUtc > NOW() - INTERVAL '30 days'
    GROUP BY UserId
)
SELECT u.FullName, ro.OrderCount
FROM   dbo.ExampleUsers u
INNER JOIN recent_orders ro ON ro.UserId = u.Id
WHERE  u.IsActive = true;
```

### CTEs encadenadas

```sql
WITH
active_users AS (
    SELECT Id, FullName, Email
    FROM   dbo.ExampleUsers
    WHERE  IsActive = true AND DeletedAt IS NULL
),
users_with_orders AS (
    SELECT u.Id, u.FullName, COUNT(o.Id) AS OrderCount
    FROM   active_users u
    LEFT JOIN dbo.ExampleOrders o ON o.UserId = u.Id
    GROUP BY u.Id, u.FullName
),
top_customers AS (
    SELECT * FROM users_with_orders
    WHERE  OrderCount >= 5
)
SELECT FullName, OrderCount
FROM   top_customers
ORDER BY OrderCount DESC
LIMIT 10;
```

### CTE recursiva — jerarquías

```sql
-- Tabla de categorías con self-reference
-- Categories: Id, Name, ParentId (NULL si es raíz)

WITH RECURSIVE category_tree AS (
    -- Caso base: categorías raíz
    SELECT Id, Name, ParentId, 0 AS Depth, Name::TEXT AS Path
    FROM   dbo.Categories
    WHERE  ParentId IS NULL

    UNION ALL

    -- Caso recursivo: hijos
    SELECT c.Id, c.Name, c.ParentId, ct.Depth + 1, ct.Path || ' > ' || c.Name
    FROM   dbo.Categories c
    INNER JOIN category_tree ct ON ct.Id = c.ParentId
)
SELECT Depth, Path
FROM   category_tree
ORDER BY Path;
```

---

## Window Functions — cálculos por grupos sin colapsar filas

A diferencia de `GROUP BY`, las window functions calculan sobre un grupo pero mantienen todas las filas:

```sql
-- ROW_NUMBER() — numeración por partición
SELECT PublicId, FullName, CreatedAtUtc,
       ROW_NUMBER() OVER (ORDER BY CreatedAtUtc ASC) AS RowNum
FROM   dbo.ExampleUsers
WHERE  DeletedAt IS NULL;

-- RANK() — igual que ROW_NUMBER pero con huecos en empates
-- DENSE_RANK() — igual pero sin huecos

-- Partición — ROW_NUMBER dentro de cada grupo
SELECT UserId, Total, CreatedAtUtc,
       ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY CreatedAtUtc DESC) AS OrderRank
FROM   dbo.ExampleOrders;
-- OrderRank = 1 es la orden más reciente de cada usuario

-- Obtener el último pedido de cada usuario
WITH ranked_orders AS (
    SELECT UserId, Total, CreatedAtUtc,
           ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY CreatedAtUtc DESC) AS rn
    FROM   dbo.ExampleOrders
)
SELECT UserId, Total, CreatedAtUtc
FROM   ranked_orders
WHERE  rn = 1;

-- LAG / LEAD — acceder a filas anteriores/siguientes
SELECT CreatedAtUtc, Total,
       LAG(Total)  OVER (ORDER BY CreatedAtUtc) AS PreviousOrder,
       LEAD(Total) OVER (ORDER BY CreatedAtUtc) AS NextOrder
FROM   dbo.ExampleOrders
WHERE  UserId = 1;

-- SUM / AVG acumulativo
SELECT CreatedAtUtc, Total,
       SUM(Total) OVER (ORDER BY CreatedAtUtc) AS CumulativeRevenue
FROM   dbo.ExampleOrders;
```

---

## ON CONFLICT — upsert

Insertar si no existe, actualizar si existe (sin try/catch ni check previo):

```sql
-- ON CONFLICT DO NOTHING — ignorar si ya existe
INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
VALUES (@PublicId, @FullName, @Email, true, now(), now())
ON CONFLICT (Email) DO NOTHING;

-- ON CONFLICT DO UPDATE — actualizar si ya existe
INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
VALUES (@PublicId, @FullName, @Email, true, now(), now())
ON CONFLICT (Email)
DO UPDATE SET
    FullName     = EXCLUDED.FullName,    -- EXCLUDED = los valores que se intentaron insertar
    UpdatedAtUtc = now();

-- ON CONFLICT en columna específica
ON CONFLICT (PublicId) DO UPDATE SET ...

-- ON CONFLICT en constraint nombrado
ON CONFLICT ON CONSTRAINT uix_example_users_email DO UPDATE SET ...
```

### Upsert en Dapper

```csharp
public Task UpsertAsync(ExampleUser user, CancellationToken ct) =>
    _db.ExecuteAsync(
        """
        INSERT INTO dbo.ExampleUsers (PublicId, FullName, Email, IsActive, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@PublicId, @FullName, @Email, true, now(), now())
        ON CONFLICT (Email)
        DO UPDATE SET
            FullName     = EXCLUDED.FullName,
            UpdatedAtUtc = now()
        WHERE dbo.ExampleUsers.DeletedAt IS NULL;
        """,
        new { user.PublicId, user.FullName, user.Email },
        cancellationToken: ct);
```

---

## JSONB — datos semi-estructurados

PostgreSQL tiene soporte nativo para JSON con indexación y operadores:

```sql
-- Columna JSONB
ALTER TABLE dbo.ExampleUsers ADD COLUMN IF NOT EXISTS Metadata JSONB;

-- Insertar JSON
UPDATE dbo.ExampleUsers
SET Metadata = '{"plan": "premium", "locale": "es-MX", "features": ["exports", "api"]}'
WHERE PublicId = @publicId;

-- Operadores JSONB
SELECT Metadata->'plan'           FROM dbo.ExampleUsers;  -- retorna JSON ("premium")
SELECT Metadata->>'plan'          FROM dbo.ExampleUsers;  -- retorna texto  (premium)
SELECT Metadata->'features'->0    FROM dbo.ExampleUsers;  -- primer elemento del array
SELECT Metadata#>>'{address,city}' FROM dbo.ExampleUsers; -- path anidado

-- Filtrar por valor JSONB
SELECT * FROM dbo.ExampleUsers WHERE Metadata->>'plan' = 'premium';
SELECT * FROM dbo.ExampleUsers WHERE Metadata @> '{"plan": "premium"}';  -- contiene

-- ¿Tiene la clave?
SELECT * FROM dbo.ExampleUsers WHERE Metadata ? 'plan';

-- Actualizar un campo sin reescribir todo el JSON
UPDATE dbo.ExampleUsers
SET Metadata = Metadata || '{"plan": "enterprise"}'
WHERE PublicId = @publicId;
```

---

## Funciones de fecha — las más usadas

```sql
-- Fecha/hora actual
SELECT NOW();                     -- timestamp con timezone
SELECT CURRENT_TIMESTAMP;         -- equivalente
SELECT timezone('utc', now());    -- UTC (siempre usar esto en el proyecto)

-- Truncar a unidad de tiempo
SELECT DATE_TRUNC('day',   NOW());    -- inicio del día
SELECT DATE_TRUNC('month', NOW());    -- inicio del mes
SELECT DATE_TRUNC('year',  NOW());    -- inicio del año
SELECT DATE_TRUNC('hour',  NOW());    -- inicio de la hora

-- Aritmética de fechas
SELECT NOW() + INTERVAL '7 days';
SELECT NOW() - INTERVAL '1 month';
SELECT '2025-12-31'::DATE - '2025-01-01'::DATE;  -- diferencia en días (364)

-- Extraer partes
SELECT EXTRACT(YEAR  FROM NOW());   -- 2025
SELECT EXTRACT(MONTH FROM NOW());   -- 5
SELECT EXTRACT(DOW   FROM NOW());   -- día de semana (0=domingo)

-- Comparar solo fechas (ignorar hora)
SELECT * FROM dbo.ExampleUsers
WHERE DATE(CreatedAtUtc) = CURRENT_DATE;
```

---

## VACUUM y mantenimiento

PostgreSQL usa MVCC (Multi-Version Concurrency Control): los UPDATE y DELETE no borran filas inmediatamente, sino que crean nuevas versiones. Las versiones viejas se llaman "dead tuples" y ocupan espacio.

```sql
-- VACUUM — limpia dead tuples y libera espacio para reutilización interna
VACUUM dbo.ExampleUsers;

-- VACUUM FULL — compacta la tabla (requiere lock exclusivo — no en producción sin ventana)
VACUUM FULL dbo.ExampleUsers;

-- ANALYZE — actualiza las estadísticas de distribución de datos (mejora el planner)
ANALYZE dbo.ExampleUsers;

-- VACUUM ANALYZE — hacer ambos a la vez
VACUUM ANALYZE dbo.ExampleUsers;

-- Ver el estado de las tablas (dead tuples acumuladas)
SELECT schemaname, tablename, n_dead_tup, n_live_tup,
       last_vacuum, last_autovacuum
FROM pg_stat_user_tables
ORDER BY n_dead_tup DESC;
```

**autovacuum:** PostgreSQL tiene un proceso automático que ejecuta VACUUM en segundo plano. En la mayoría de casos no se necesita intervención manual. Si `n_dead_tup` es muy alto, revisar la configuración de autovacuum.

---

## Relación con back-template

Los patterns de este documento aparecen en las clases `...Sql`:

```csharp
// CTE en una query compleja de listado con conteo
public Task<IEnumerable<UserSummaryDto>> GetSummaryAsync(CancellationToken ct) =>
    _db.QueryAsync<UserSummaryDto>(
        """
        WITH user_stats AS (
            SELECT UserId, COUNT(*) AS OrderCount, SUM(Total) AS TotalSpent
            FROM   dbo.ExampleOrders
            GROUP BY UserId
        )
        SELECT u.PublicId, u.FullName, COALESCE(s.OrderCount, 0) AS OrderCount
        FROM   dbo.ExampleUsers u
        LEFT JOIN user_stats s ON s.UserId = u.Id
        WHERE  u.DeletedAt IS NULL
        ORDER BY u.FullName;
        """,
        cancellationToken: ct);
```

---

## EXPLAIN ANALYZE — entender el plan de ejecución
> Fuente: *Empezando con PostgreSQL* — Ch.9 Optimización de consultas

`EXPLAIN ANALYZE` muestra el plan de ejecución real de una query: qué índices usa, cuántas filas lee, cuánto tarda.

```sql
-- EXPLAIN — muestra el plan estimado (no ejecuta la query)
EXPLAIN
SELECT * FROM ExampleUsers WHERE Email = 'usuario@test.com';

-- EXPLAIN ANALYZE — ejecuta la query y muestra el plan real con tiempos
EXPLAIN ANALYZE
SELECT * FROM ExampleUsers WHERE Email = 'usuario@test.com';

-- EXPLAIN (ANALYZE, BUFFERS) — incluye información de caché de disco
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT u.FullName, COUNT(o.Id) AS Ordenes
FROM ExampleUsers u
LEFT JOIN ExampleOrders o ON o.UserId = u.Id
WHERE u.IsActive = true
GROUP BY u.Id, u.FullName;
```

### Cómo leer la salida

```
Seq Scan on ExampleUsers (cost=0.00..245.00 rows=1 width=120)
             ↑                      ↑          ↑         ↑
         tipo de scan          costo estimado  filas   bytes por fila

Index Scan using idx_users_email on ExampleUsers
  Index Cond: (email = 'usuario@test.com')
  Actual time=0.042..0.044. rows=1 loops=1   ← tiempo real, filas reales

Seq Scan = tabla completa (malo en tablas grandes)
Index Scan = usa un índice (bueno)
Bitmap Heap Scan = combina índice + heap (para múltiples filas)
```

### Señales de alerta en EXPLAIN

```
Seq Scan en tabla grande          → falta índice para el campo filtrado
Hash Join con filas estimadas >>  → estadísticas desactualizadas (ANALYZE)
Nested Loop con many rows         → considerar índice en la columna del JOIN
actual rows >> estimated rows     → estadísticas desactualizadas
```

```sql
-- Después de un INSERT masivo o cambio estructural importante
ANALYZE ExampleUsers;   -- actualiza las estadísticas para que el planner funcione bien
```

---

## Particionamiento de tablas

Para tablas que crecen mucho (logs, eventos, auditoría), PostgreSQL permite particionarlas por rango o lista.

```sql
-- Tabla particionada por rango de fecha (logs por mes)
CREATE TABLE AuditLogs (
    Id          UUID          NOT NULL DEFAULT gen_random_uuid(),
    UserId      UUID          NOT NULL,
    Action      VARCHAR(100)  NOT NULL,
    OccurredAt  TIMESTAMPTZ   NOT NULL
) PARTITION BY RANGE (OccurredAt);

-- Crear particiones por mes
CREATE TABLE AuditLogs_2026_01
    PARTITION OF AuditLogs
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE AuditLogs_2026_02
    PARTITION OF AuditLogs
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');

-- Las queries con filtro por fecha solo leen la partición relevante
SELECT * FROM AuditLogs
WHERE OccurredAt BETWEEN '2026-01-15' AND '2026-01-20';
-- PostgreSQL solo lee AuditLogs_2026_01 → mucho más rápido
```

```sql
-- Tabla particionada por lista (por tenant en SaaS)
CREATE TABLE ExampleUsersPartitioned (
    Id       UUID         NOT NULL,
    TenantId UUID         NOT NULL,
    FullName VARCHAR(200) NOT NULL
) PARTITION BY LIST (TenantId);

-- Una partición por tenant grande
CREATE TABLE ExampleUsers_TenantA
    PARTITION OF ExampleUsersPartitioned
    FOR VALUES IN ('00000000-0000-0000-0000-000000000001');

-- DEFAULT para los demás tenants
CREATE TABLE ExampleUsers_Default
    PARTITION OF ExampleUsersPartitioned DEFAULT;
```

**Cuándo particionar:**
- Tabla con >10 millones de filas que crece continuamente (logs, eventos)
- Necesitas eliminar datos históricos eficientemente (`DROP TABLE partition` es instantáneo, `DELETE` es lento)
- Las queries casi siempre filtran por la columna de particionamiento


---

*Rogelio Arriaga Gonzalez*
