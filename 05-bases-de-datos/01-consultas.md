# 01 — SQL: Consultas Fundamentales

Las operaciones de lectura de datos: SELECT, filtros, joins, agrupaciones y ordenamiento.

---

## SELECT — leer columnas

```sql
-- Todo (evitar en producción — trae columnas innecesarias)
SELECT * FROM dbo.ExampleUsers;

-- Columnas específicas (siempre preferir esto)
SELECT PublicId, FullName, Email, IsActive
FROM   dbo.ExampleUsers;

-- Con alias
SELECT PublicId     AS id,
       FullName     AS name,
       CreatedAtUtc AS created_at
FROM   dbo.ExampleUsers;
```

---

## WHERE — filtrar filas

```sql
-- Igualdad
SELECT * FROM dbo.ExampleUsers WHERE IsActive = true;

-- Desigualdad
SELECT * FROM dbo.ExampleUsers WHERE IsActive <> true;

-- Comparaciones
SELECT * FROM dbo.ExampleUsers WHERE CreatedAtUtc > '2025-01-01';

-- LIKE — buscar por patrón
SELECT * FROM dbo.ExampleUsers WHERE FullName LIKE 'John%';    -- empieza con John
SELECT * FROM dbo.ExampleUsers WHERE Email    LIKE '%@gmail%'; -- contiene @gmail

-- IN — lista de valores
SELECT * FROM dbo.ExampleUsers WHERE PublicId IN (
    '00000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000002'
);

-- BETWEEN — rango inclusivo
SELECT * FROM dbo.ExampleUsers
WHERE CreatedAtUtc BETWEEN '2025-01-01' AND '2025-12-31';

-- NULL checks — siempre IS NULL / IS NOT NULL, nunca = NULL
SELECT * FROM dbo.ExampleUsers WHERE DeletedAt IS NULL;
SELECT * FROM dbo.ExampleUsers WHERE DeletedAt IS NOT NULL;

-- Múltiples condiciones
SELECT * FROM dbo.ExampleUsers
WHERE IsActive = true
  AND DeletedAt IS NULL
  AND CreatedAtUtc > '2025-01-01';

-- OR
SELECT * FROM dbo.ExampleUsers
WHERE FullName LIKE 'John%'
   OR Email LIKE 'john%';
```

---

## ORDER BY — ordenar resultados

```sql
-- Ascendente (por defecto)
SELECT * FROM dbo.ExampleUsers ORDER BY FullName;
SELECT * FROM dbo.ExampleUsers ORDER BY FullName ASC;

-- Descendente
SELECT * FROM dbo.ExampleUsers ORDER BY CreatedAtUtc DESC;

-- Múltiples columnas
SELECT * FROM dbo.ExampleUsers
ORDER BY IsActive DESC, CreatedAtUtc DESC;

-- Con NULLS — control de dónde van los NULLs
SELECT * FROM dbo.ExampleUsers
ORDER BY DeletedAt NULLS LAST;
```

---

## LIMIT y OFFSET — paginación

```sql
-- Primeras 10 filas
SELECT * FROM dbo.ExampleUsers
ORDER BY CreatedAtUtc DESC
LIMIT 10;

-- Página 2 (filas 11-20)
SELECT * FROM dbo.ExampleUsers
ORDER BY CreatedAtUtc DESC
LIMIT 10 OFFSET 10;

-- Fórmula para paginación: OFFSET = (page - 1) * pageSize
-- page=1, pageSize=10 → OFFSET 0
-- page=2, pageSize=10 → OFFSET 10
-- page=3, pageSize=10 → OFFSET 20
```

**Problema de OFFSET:** en tablas grandes, `OFFSET 100000` obliga a PostgreSQL a leer y descartar 100,000 filas. Para paginación de grandes volúmenes, usar keyset pagination (ver `api-design/03-paginacion.md`).

---

## JOIN — combinar tablas

```sql
-- Datos de ejemplo
-- ExampleUsers: Id, PublicId, FullName, Email
-- ExampleOrders: Id, UserId (FK → ExampleUsers.Id), Total, CreatedAtUtc

-- INNER JOIN — solo filas que hacen match en AMBAS tablas
SELECT u.PublicId, u.FullName, o.Total
FROM   dbo.ExampleUsers u
INNER JOIN dbo.ExampleOrders o ON o.UserId = u.Id
WHERE  u.IsActive = true;

-- LEFT JOIN — todos los usuarios, con o sin órdenes
SELECT u.PublicId, u.FullName, COUNT(o.Id) AS OrderCount
FROM   dbo.ExampleUsers u
LEFT JOIN dbo.ExampleOrders o ON o.UserId = u.Id
WHERE  u.DeletedAt IS NULL
GROUP BY u.PublicId, u.FullName;

-- RIGHT JOIN — todas las órdenes, con o sin usuario (raro, preferir LEFT JOIN)
-- FULL OUTER JOIN — todas las filas de ambas tablas

-- Multiple JOINs
SELECT u.FullName, o.Total, p.Name AS ProductName
FROM   dbo.ExampleUsers u
INNER JOIN dbo.ExampleOrders o ON o.UserId = u.Id
INNER JOIN dbo.ExampleOrderItems oi ON oi.OrderId = o.Id
INNER JOIN dbo.ExampleProducts p ON p.Id = oi.ProductId
WHERE  u.DeletedAt IS NULL;
```

### INNER vs LEFT — regla de decisión

```
¿Quiero solo filas que tienen relación? → INNER JOIN
¿Quiero todas las filas aunque no tengan relación? → LEFT JOIN

Ejemplo: listar todos los usuarios y su cantidad de órdenes (incluyendo usuarios sin órdenes)
→ LEFT JOIN (los usuarios sin órdenes aparecen con COUNT = 0)

Ejemplo: listar solo usuarios que tienen al menos una orden
→ INNER JOIN (los usuarios sin órdenes no aparecen)
```

---

## GROUP BY + funciones de agregación

```sql
-- COUNT — contar filas
SELECT COUNT(*) AS Total FROM dbo.ExampleUsers;
SELECT COUNT(*) AS Active FROM dbo.ExampleUsers WHERE IsActive = true;
SELECT COUNT(DISTINCT Email) AS UniqueEmails FROM dbo.ExampleUsers;

-- SUM, AVG, MIN, MAX
SELECT SUM(Total) AS Revenue FROM dbo.ExampleOrders;
SELECT AVG(Total) AS AvgOrder FROM dbo.ExampleOrders;
SELECT MIN(CreatedAtUtc) AS FirstOrder FROM dbo.ExampleOrders;

-- GROUP BY — agrupar y agregar
SELECT u.FullName, COUNT(o.Id) AS OrderCount, SUM(o.Total) AS TotalSpent
FROM   dbo.ExampleUsers u
LEFT JOIN dbo.ExampleOrders o ON o.UserId = u.Id
WHERE  u.DeletedAt IS NULL
GROUP BY u.Id, u.FullName
ORDER BY TotalSpent DESC;

-- HAVING — filtrar después de GROUP BY (WHERE no puede usar funciones de agregación)
SELECT u.FullName, COUNT(o.Id) AS OrderCount
FROM   dbo.ExampleUsers u
INNER JOIN dbo.ExampleOrders o ON o.UserId = u.Id
GROUP BY u.Id, u.FullName
HAVING COUNT(o.Id) >= 5;   -- solo usuarios con 5 o más órdenes
```

---

## Subqueries

```sql
-- Subquery en WHERE
SELECT * FROM dbo.ExampleUsers
WHERE Id IN (
    SELECT UserId FROM dbo.ExampleOrders
    WHERE Total > 1000
);

-- Subquery correlacionada — se ejecuta por cada fila del outer query
SELECT u.FullName,
       (SELECT COUNT(*) FROM dbo.ExampleOrders o WHERE o.UserId = u.Id) AS OrderCount
FROM   dbo.ExampleUsers u
WHERE  u.DeletedAt IS NULL;

-- Subquery en FROM (derived table)
SELECT avg_data.AvgTotal, avg_data.Month
FROM (
    SELECT AVG(Total) AS AvgTotal,
           DATE_TRUNC('month', CreatedAtUtc) AS Month
    FROM   dbo.ExampleOrders
    GROUP BY Month
) AS avg_data
ORDER BY avg_data.Month;
```

---

## DISTINCT — eliminar duplicados

```sql
SELECT DISTINCT Email FROM dbo.ExampleUsers;

-- Con múltiples columnas — fila es única si la combinación es única
SELECT DISTINCT FullName, Email FROM dbo.ExampleUsers;
```

---

## EXISTS — verificar existencia (más eficiente que IN para subqueries correlacionadas)

```sql
-- ¿El email ya existe?
SELECT EXISTS(
    SELECT 1 FROM dbo.ExampleUsers WHERE Email = 'test@test.com'
);

-- Usuarios que tienen al menos una orden
SELECT * FROM dbo.ExampleUsers u
WHERE EXISTS (
    SELECT 1 FROM dbo.ExampleOrders o WHERE o.UserId = u.Id
);
```

---

## Relación con back-template

Todos estos patterns se usan en las clases `...Sql` del proyecto. Ejemplos directos:

```csharp
// Infrastructure/Persistence/SQLDB/Main/ExampleUsers/ExampleUsersSql.cs

// SELECT con filtros y paginación
public Task<IEnumerable<ExampleUser>> GetPagedAsync(int page, int pageSize, CancellationToken ct) =>
    _db.QueryAsync<ExampleUser>(
        """
        SELECT Id, PublicId, FullName, Email, IsActive, CreatedAtUtc
        FROM   dbo.ExampleUsers
        WHERE  DeletedAt IS NULL
        ORDER BY CreatedAtUtc DESC
        LIMIT  @pageSize OFFSET @offset;
        """,
        new { pageSize, offset = (page - 1) * pageSize },
        cancellationToken: ct);

// COUNT para paginación
public Task<int> CountActiveAsync(CancellationToken ct) =>
    _db.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM dbo.ExampleUsers WHERE DeletedAt IS NULL;",
        cancellationToken: ct);

// EXISTS para verificar unicidad
public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
    _db.ExecuteScalarAsync<bool>(
        "SELECT EXISTS(SELECT 1 FROM dbo.ExampleUsers WHERE Email = @email AND DeletedAt IS NULL);",
        new { email }, cancellationToken: ct);
```

---

## CTEs — Common Table Expressions (WITH)
> Fuente: *Fundamentos de SQL* — Ch.7 Consultas Avanzadas

Las CTEs hacen legibles las consultas complejas separando partes lógicas.

```sql
-- CTE básico — nombra una subquery para referenciarla después
WITH UsuariosActivos AS (
    SELECT Id, PublicId, FullName, Email
    FROM   ExampleUsers
    WHERE  IsActive = true
      AND  DeletedAt IS NULL
),
OrdenesRecientes AS (
    SELECT UserId, COUNT(*) AS TotalOrdenes, SUM(Total) AS TotalGastado
    FROM   ExampleOrders
    WHERE  CreatedAtUtc >= NOW() - INTERVAL '30 days'
    GROUP BY UserId
)
SELECT
    ua.FullName,
    ua.Email,
    COALESCE(or2.TotalOrdenes, 0) AS OrdenesUltimos30Dias,
    COALESCE(or2.TotalGastado, 0) AS GastadoUltimos30Dias
FROM   UsuariosActivos ua
LEFT JOIN OrdenesRecientes or2 ON or2.UserId = ua.Id
ORDER BY or2.TotalGastado DESC NULLS LAST;
```

```sql
-- CTE recursivo — para estructuras jerárquicas (árbol de categorías)
WITH RECURSIVE Categorias AS (
    -- Base: categorías raíz (sin padre)
    SELECT Id, Nombre, ParentId, 0 AS Nivel
    FROM   Categorias
    WHERE  ParentId IS NULL

    UNION ALL

    -- Recursivo: hijos de cada categoría
    SELECT c.Id, c.Nombre, c.ParentId, ca.Nivel + 1
    FROM   Categorias c
    INNER JOIN Categorias ca ON c.ParentId = ca.Id
)
SELECT * FROM Categorias ORDER BY Nivel, Nombre;
```

---

## Window Functions — cálculos sobre ventanas de filas

Las window functions calculan un valor por fila usando un grupo de filas relacionadas — sin colapsar las filas como GROUP BY.

```sql
-- ROW_NUMBER — numerar filas dentro de cada grupo
SELECT
    FullName,
    Email,
    CreatedAtUtc,
    ROW_NUMBER() OVER (ORDER BY CreatedAtUtc DESC) AS RankGlobal
FROM ExampleUsers
WHERE IsActive = true;

-- RANK y DENSE_RANK — rankear con empates
SELECT
    u.FullName,
    SUM(o.Total) AS TotalGastado,
    RANK()       OVER (ORDER BY SUM(o.Total) DESC) AS Rank,        -- saltea números en empates
    DENSE_RANK() OVER (ORDER BY SUM(o.Total) DESC) AS DenseRank    -- no saltea números
FROM ExampleUsers u
INNER JOIN ExampleOrders o ON o.UserId = u.Id
GROUP BY u.Id, u.FullName;

-- PARTITION BY — reinicia el cálculo por grupo
SELECT
    u.FullName,
    o.CreatedAtUtc,
    o.Total,
    ROW_NUMBER() OVER (
        PARTITION BY u.Id          -- reiniciar numeración por usuario
        ORDER BY o.CreatedAtUtc DESC
    ) AS OrdenNumero
FROM ExampleUsers u
INNER JOIN ExampleOrders o ON o.UserId = u.Id;

-- LAG / LEAD — acceder a la fila anterior/siguiente
SELECT
    CreatedAtUtc,
    Total,
    LAG(Total)  OVER (ORDER BY CreatedAtUtc) AS TotalAnterior,
    Total - LAG(Total) OVER (ORDER BY CreatedAtUtc) AS Variacion
FROM ExampleOrders
WHERE UserId = '00000000-0000-0000-0000-000000000001';

-- SUM acumulativo (running total)
SELECT
    CreatedAtUtc,
    Total,
    SUM(Total) OVER (ORDER BY CreatedAtUtc ROWS UNBOUNDED PRECEDING) AS TotalAcumulado
FROM ExampleOrders
ORDER BY CreatedAtUtc;
```

```csharp
// Window function desde Dapper — el resultado es un DTO específico, no la entidad
public Task<IEnumerable<UserOrderRankDto>> GetUserOrderRankingAsync(CancellationToken ct) =>
    _db.QueryAsync<UserOrderRankDto>(
        """
        SELECT
            u.PublicId,
            u.FullName,
            COALESCE(SUM(o.Total), 0)  AS TotalGastado,
            RANK() OVER (ORDER BY COALESCE(SUM(o.Total), 0) DESC) AS Posicion
        FROM ExampleUsers u
        LEFT JOIN ExampleOrders o ON o.UserId = u.Id
        WHERE u.IsActive = true
        GROUP BY u.Id, u.PublicId, u.FullName
        ORDER BY Posicion;
        """,
        cancellationToken: ct);
```


---

*Rogelio Arriaga Gonzalez*
