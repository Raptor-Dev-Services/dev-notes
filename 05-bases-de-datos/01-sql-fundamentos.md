# 01 — SQL Fundamentos

SQL (Structured Query Language) es el lenguaje estándar para consultar y manipular bases de datos relacionales. Dominar SQL es prerequisito para usar correctamente Dapper, EF Core y para diagnosticar problemas de rendimiento en producción.

> Fuente: *Learning SQL* — Alan Beaulieu (O'Reilly); Documentación oficial PostgreSQL 16

---

## Anatomía de una SELECT

```sql
SELECT   u.public_id, u.full_name, u.email, t.name AS tenant_name
FROM     users u
JOIN     tenants t ON t.id = u.tenant_id
WHERE    u.is_active = true
  AND    u.tenant_id = '3fa85f64-5717-4562-b3fc-2c963f66afa6'
ORDER BY u.full_name ASC
LIMIT    20 OFFSET 0;

-- Orden de ejecución (no es el orden de escritura):
-- 1. FROM + JOIN   → forma el dataset base
-- 2. WHERE         → filtra filas
-- 3. GROUP BY      → agrupa (si hay)
-- 4. HAVING        → filtra grupos (si hay)
-- 5. SELECT        → elige columnas
-- 6. ORDER BY      → ordena
-- 7. LIMIT/OFFSET  → pagina
```

---

## JOINs

```sql
-- INNER JOIN — solo filas con match en ambas tablas
SELECT u.full_name, r.name AS role
FROM   users u
INNER JOIN user_roles ur ON ur.user_id = u.id
INNER JOIN roles r       ON r.id = ur.role_id;

-- LEFT JOIN — todas las filas de la izquierda, NULLs donde no hay match
SELECT u.full_name, p.plan_name
FROM   users u
LEFT JOIN subscriptions s ON s.user_id = u.id
LEFT JOIN plans p         ON p.id = s.plan_id;
-- usuarios sin suscripción aparecen con plan_name = NULL

-- RIGHT JOIN — todas las filas de la derecha (raro, se prefiere LEFT JOIN)

-- FULL OUTER JOIN — todas las filas de ambas tablas, NULLs donde no hay match
SELECT u.full_name, o.order_number
FROM   users u
FULL OUTER JOIN orders o ON o.user_id = u.id;

-- CROSS JOIN — producto cartesiano (cada fila izq × cada fila der)
SELECT t.name, p.name
FROM   tenants t
CROSS JOIN plans p;  -- útil para combinatorias, peligroso en tablas grandes
```

---

## Funciones de agregación y GROUP BY

```sql
-- Contar usuarios activos por tenant
SELECT   tenant_id,
         COUNT(*)                     AS total_users,
         COUNT(*) FILTER (WHERE is_active) AS active_users,
         MAX(created_at)              AS last_signup
FROM     users
GROUP BY tenant_id
HAVING   COUNT(*) > 5            -- filtra grupos, no filas individuales
ORDER BY total_users DESC;

-- Funciones de agregación:
-- COUNT(*), COUNT(col) — cuenta filas / valores no NULL
-- SUM(col)             — suma
-- AVG(col)             — promedio
-- MIN(col), MAX(col)   — mínimo y máximo
-- STRING_AGG(col, ',') — concatena strings de un grupo (PostgreSQL)
-- ARRAY_AGG(col)       — agrupa valores en un array (PostgreSQL)
```

---

## CTEs (Common Table Expressions)

```sql
-- WITH nombre AS (...) — subconsulta nombrada y reutilizable
WITH active_users AS (
    SELECT id, full_name, tenant_id
    FROM   users
    WHERE  is_active = true
),
tenant_counts AS (
    SELECT   tenant_id, COUNT(*) AS active_count
    FROM     active_users
    GROUP BY tenant_id
)
SELECT t.name, tc.active_count
FROM   tenants t
JOIN   tenant_counts tc ON tc.tenant_id = t.id
ORDER  BY tc.active_count DESC;

-- CTE recursiva — árbol de categorías o jerarquía de managers
WITH RECURSIVE org_chart AS (
    -- Anchor: empleados sin manager (raíz)
    SELECT id, full_name, manager_id, 0 AS level
    FROM   employees
    WHERE  manager_id IS NULL

    UNION ALL

    -- Recursión: empleados cuyo manager ya está en el CTE
    SELECT e.id, e.full_name, e.manager_id, oc.level + 1
    FROM   employees e
    JOIN   org_chart oc ON oc.id = e.manager_id
)
SELECT level, full_name FROM org_chart ORDER BY level, full_name;
```

---

## Window Functions (funciones de ventana)

No colapsan filas como GROUP BY. Calculan sobre un conjunto sin agrupar.

```sql
-- ROW_NUMBER — numeración única por partición
SELECT
    full_name,
    tenant_id,
    created_at,
    ROW_NUMBER() OVER (PARTITION BY tenant_id ORDER BY created_at ASC) AS user_number
FROM users;
-- user_number reinicia en 1 para cada tenant

-- RANK / DENSE_RANK — ranking con y sin huecos en empates
SELECT
    full_name,
    score,
    RANK()       OVER (ORDER BY score DESC) AS rank,        -- 1,2,2,4 (hueco en 3)
    DENSE_RANK() OVER (ORDER BY score DESC) AS dense_rank   -- 1,2,2,3 (sin hueco)
FROM leaderboard;

-- LAG / LEAD — accede a la fila anterior/siguiente
SELECT
    month,
    revenue,
    LAG(revenue)  OVER (ORDER BY month) AS prev_month_revenue,
    LEAD(revenue) OVER (ORDER BY month) AS next_month_revenue,
    revenue - LAG(revenue) OVER (ORDER BY month) AS delta
FROM monthly_revenue;

-- SUM / AVG acumulativo
SELECT
    created_at::date AS day,
    amount,
    SUM(amount) OVER (ORDER BY created_at) AS cumulative_total
FROM payments;
```

---

## LIKE, DISTINCT y operadores adicionales

```sql
-- LIKE — buscar por patrón (% = cualquier cadena, _ = un carácter)
SELECT * FROM users WHERE full_name LIKE 'John%';      -- empieza con John
SELECT * FROM users WHERE email     LIKE '%@gmail%';   -- contiene @gmail
SELECT * FROM users WHERE email     ILIKE '%@GMAIL%';  -- ILIKE = case-insensitive (PostgreSQL)

-- IN — lista de valores (más legible que múltiples OR)
SELECT * FROM users WHERE public_id IN ('id-1', 'id-2', 'id-3');

-- BETWEEN — rango inclusivo
SELECT * FROM users WHERE created_at BETWEEN '2025-01-01' AND '2025-12-31';

-- DISTINCT — eliminar filas duplicadas
SELECT DISTINCT email FROM users;
SELECT DISTINCT full_name, tenant_id FROM users;   -- única por combinación

-- ORDER BY con NULLS LAST (PostgreSQL)
SELECT * FROM users ORDER BY deleted_at NULLS LAST;
```

---

## Subqueries

```sql
-- Subquery en WHERE
SELECT full_name, email
FROM   users
WHERE  tenant_id IN (
    SELECT id FROM tenants WHERE plan_id = 'plan-enterprise'
);

-- Subquery correlacionada — se evalúa por cada fila de la query exterior
SELECT u.full_name,
       (SELECT COUNT(*) FROM orders o WHERE o.user_id = u.id) AS order_count
FROM   users u;
-- Atención: puede ser lenta; a menudo se reemplaza por LEFT JOIN + GROUP BY

-- EXISTS — más eficiente que IN cuando hay muchos valores
SELECT full_name
FROM   users u
WHERE  EXISTS (
    SELECT 1 FROM orders o
    WHERE  o.user_id = u.id AND o.status = 'pending'
);
```

---

## Transacciones (ACID)

```sql
BEGIN;

UPDATE accounts SET balance = balance - 500 WHERE id = 'user-a';
UPDATE accounts SET balance = balance + 500 WHERE id = 'user-b';

-- Si algo falla antes del COMMIT, ningún cambio persiste
COMMIT;

-- ROLLBACK explícito en caso de error
BEGIN;
UPDATE inventory SET quantity = quantity - 1 WHERE product_id = 'P001';
-- Detecto que quantity quedó negativo:
ROLLBACK;  -- deshace todo desde el BEGIN
```

| Propiedad ACID | Significado |
|----------------|-------------|
| **Atomicity** | Todo o nada — si falla cualquier operación, se deshace todo |
| **Consistency** | La DB pasa de un estado válido a otro — constraints no se violan |
| **Isolation** | Transacciones concurrentes no se ven entre sí hasta el COMMIT |
| **Durability** | Una vez commiteado, el cambio sobrevive a reinicios |

---

## Índices en PostgreSQL

```sql
-- B-Tree (default) — ideal para =, <, >, BETWEEN, ORDER BY
CREATE INDEX idx_users_tenant_id ON users(tenant_id);
CREATE INDEX idx_users_email     ON users(email);

-- Índice compuesto — el orden importa: filtra primero por tenant_id, luego email
CREATE INDEX idx_users_tenant_email ON users(tenant_id, email);
-- Útil para: WHERE tenant_id = ? AND email = ?
-- No útil para: WHERE email = ? (sin tenant_id al frente)

-- Índice parcial — solo indexa filas que cumplen la condición
CREATE INDEX idx_users_active ON users(tenant_id) WHERE is_active = true;
-- Más pequeño y rápido que indexar toda la tabla

-- Índice único — garantiza unicidad + índice de búsqueda
CREATE UNIQUE INDEX idx_users_email_tenant ON users(email, tenant_id);

-- EXPLAIN ANALYZE — ver si el índice se usa
EXPLAIN ANALYZE
SELECT * FROM users WHERE tenant_id = '...' AND is_active = true;
-- Buscar: "Index Scan" (bueno) vs "Seq Scan" (puede ser problema en tablas grandes)
```

---

## NULL y comparaciones

```sql
-- ❌ NULL no se compara con = — siempre retorna NULL (falso)
SELECT * FROM users WHERE deleted_at = NULL;    -- nunca retorna filas

-- ✓ Usar IS NULL / IS NOT NULL
SELECT * FROM users WHERE deleted_at IS NULL;   -- usuarios no eliminados

-- COALESCE — primer valor no NULL
SELECT COALESCE(nickname, full_name, email) AS display_name FROM users;

-- NULLIF — retorna NULL si los dos argumentos son iguales
SELECT NULLIF(quantity, 0) AS safe_quantity FROM inventory;
-- Evita división por cero: price / NULLIF(quantity, 0)
```

---

## Relación con el back-template

En el back-template, las consultas SQL se escriben en clases `...Sql`:

```csharp
public static class ExampleUsersSql
{
    public const string GetByPublicId = """
        SELECT  u.id, u.public_id, u.full_name, u.email, u.is_active
        FROM    users u
        WHERE   u.public_id = @PublicId
          AND   u.tenant_id = @TenantId
          AND   u.deleted_at IS NULL
        """;

    public const string GetPaged = """
        SELECT  u.id, u.public_id, u.full_name, u.email, u.is_active,
                COUNT(*) OVER() AS total_count
        FROM    users u
        WHERE   u.tenant_id = @TenantId
          AND   u.is_active = true
        ORDER BY u.full_name ASC
        LIMIT   @PageSize OFFSET @Offset
        """;
}
```

`COUNT(*) OVER()` es una window function que retorna el total sin una segunda query. Es el patrón estándar para paginación eficiente con Dapper.

---

## Cuándo usar / no usar SQL directo

| Usar SQL directo (Dapper) | Usar ORM (EF Core) |
|--------------------------|-------------------|
| Queries complejas con JOINs, CTEs, window functions | CRUD simple sin joins |
| Reports y dashboards con agregaciones | Cuando el modelo de dominio es complejo |
| Queries donde el rendimiento es crítico | Cuando se necesitan migrations automáticas |
| Multi-tenancy con Row Level Security | Prototipos rápidos |

---

## Glosario

| Término | Definición |
|---------|-----------|
| DDL | Data Definition Language — CREATE, ALTER, DROP (estructura de tablas) |
| DML | Data Manipulation Language — SELECT, INSERT, UPDATE, DELETE (datos) |
| DCL | Data Control Language — GRANT, REVOKE (permisos) |
| JOIN | Combina filas de dos tablas basándose en una condición relacionada |
| INNER JOIN | Solo retorna filas con match en ambas tablas |
| LEFT JOIN | Retorna todas las filas de la tabla izquierda, NULLs donde no hay match |
| CTE | Common Table Expression — subconsulta nombrada con `WITH nombre AS (...)` |
| Window Function | Función que opera sobre un conjunto de filas sin colapsarlas — ROW_NUMBER, SUM OVER |
| Partition BY | Divide las filas en grupos para una window function — como GROUP BY pero sin colapsar |
| B-Tree | Tipo de índice default — eficiente para comparaciones de igualdad y rango |
| Índice parcial | Índice que solo cubre un subconjunto de filas — más pequeño y eficiente |
| EXPLAIN ANALYZE | Comando PostgreSQL que muestra el plan de ejecución real con tiempos medidos |
| Seq Scan | Escaneo secuencial de toda la tabla — puede indicar índice faltante |
| Index Scan | Uso del índice para encontrar filas — generalmente más eficiente que Seq Scan |
| ACID | Atomicity, Consistency, Isolation, Durability — propiedades de una transacción segura |
| COALESCE | Función que retorna el primer valor no NULL de una lista de argumentos |
| NULLIF | Retorna NULL si los dos argumentos son iguales — útil para evitar división por cero |

---

*Rogelio Arriaga Gonzalez*
