# 02 — Índices: B-tree, Parciales, Compuestos y EXPLAIN ANALYZE

Un índice es una estructura auxiliar que permite a PostgreSQL encontrar filas sin leer toda la tabla.

> Fuente: *Procesamiento de Bases de Datos 8ed* (David M. Kroenke) — Ch.10 Database Redesign: Indexes and Performance

---

## El problema que resuelve

Sin índices, PostgreSQL hace un **sequential scan**: lee cada fila de la tabla una por una.

```sql
-- Sin índice en Email — PostgreSQL lee TODAS las filas de la tabla
SELECT * FROM dbo.ExampleUsers WHERE Email = 'john@test.com';
-- En 1,000 filas: rápido. En 10,000,000 filas: segundos.

-- Con índice en Email — PostgreSQL navega el árbol B-tree directamente
-- En cualquier tamaño: milisegundos.
```

---

## B-tree — el índice estándar

PostgreSQL crea índices B-tree por defecto. Funciona para igualdad (`=`) y rangos (`>`, `<`, `BETWEEN`, `LIKE 'prefix%'`).

```sql
-- Índice simple
CREATE INDEX IF NOT EXISTS ix_example_users_email
    ON dbo.ExampleUsers (Email);

-- PostgreSQL lo usa para:
WHERE Email = 'john@test.com'         ✓
WHERE Email LIKE 'john%'              ✓ (solo prefix, no sufijo)
WHERE Email LIKE '%john%'             ✗ (no usa el índice)
WHERE CreatedAtUtc > '2025-01-01'     ✓ (si el índice es en CreatedAtUtc)
```

---

## Índice UNIQUE — restricción + rendimiento

```sql
-- El índice UNIQUE impide duplicados Y acelera las búsquedas
CREATE UNIQUE INDEX IF NOT EXISTS uix_example_users_email
    ON dbo.ExampleUsers (Email);

-- Equivalente a la restricción de tabla:
ALTER TABLE dbo.ExampleUsers ADD CONSTRAINT uq_example_users_email UNIQUE (Email);
-- Internamente, ambos crean el mismo índice B-tree
```

---

## Índice parcial — solo indexa las filas relevantes

Más pequeño y más rápido que un índice completo cuando la mayoría de queries filtran el mismo subconjunto:

```sql
-- Solo indexa usuarios activos no eliminados
-- Si el 90% de queries tiene "WHERE DeletedAt IS NULL", este índice es mucho más pequeño
CREATE INDEX IF NOT EXISTS ix_example_users_active_email
    ON dbo.ExampleUsers (Email)
    WHERE DeletedAt IS NULL;

-- PostgreSQL usa este índice cuando la query incluye la condición del WHERE del índice
SELECT * FROM dbo.ExampleUsers WHERE Email = 'j@test.com' AND DeletedAt IS NULL;
-- ↑ Usa el índice parcial — muy eficiente

SELECT * FROM dbo.ExampleUsers WHERE Email = 'j@test.com';
-- ↑ No usa el índice parcial (falta la condición DeletedAt IS NULL) — usa el completo si existe
```

Este patrón aparece en las migraciones del proyecto:

```sql
-- Host/Services/Schema Migration/Tables/002_example_users_indexes.sql
CREATE INDEX IF NOT EXISTS ix_example_users_publicid_active
    ON dbo.ExampleUsers (PublicId)
    WHERE DeletedAt IS NULL;
```

---

## Índice compuesto — múltiples columnas

Útil cuando los queries filtran frecuentemente por más de una columna:

```sql
-- Índice en (IsActive, CreatedAtUtc) — para queries paginados de usuarios activos
CREATE INDEX IF NOT EXISTS ix_example_users_active_created
    ON dbo.ExampleUsers (IsActive, CreatedAtUtc DESC)
    WHERE DeletedAt IS NULL;

-- PostgreSQL lo usa para:
WHERE IsActive = true AND DeletedAt IS NULL ORDER BY CreatedAtUtc DESC
-- ↑ Usa el índice — coincide con el orden de columnas

-- Orden importa en índices compuestos:
-- ix_example_users (A, B) → útil para "WHERE A = x" y "WHERE A = x AND B = y"
-- ix_example_users (A, B) → NO útil para "WHERE B = y" (sin filtro por A)
```

---

## Tipos de índice

| Tipo | Cuándo usar |
|------|-------------|
| **B-tree** (default) | Igualdad, rangos, ORDER BY, LIKE con prefijo |
| **GIN** | Arrays, JSONB, full-text search |
| **GiST** | Datos geoespaciales, rangos solapados |
| **HASH** | Solo igualdad exacta (raramente mejor que B-tree) |
| **BRIN** | Tablas muy grandes con datos correlacionados (logs, time series) |

```sql
-- GIN para búsqueda en JSONB
CREATE INDEX IF NOT EXISTS ix_example_users_metadata
    ON dbo.ExampleUsers USING GIN (Metadata);

-- GIN para full-text search
CREATE INDEX IF NOT EXISTS ix_example_users_fullname_fts
    ON dbo.ExampleUsers USING GIN (to_tsvector('spanish', FullName));
```

---

## EXPLAIN ANALYZE — ver el plan de ejecución

`EXPLAIN` muestra qué hará PostgreSQL. `EXPLAIN ANALYZE` lo ejecuta y muestra el tiempo real.

```sql
-- Ver el plan sin ejecutar
EXPLAIN SELECT * FROM dbo.ExampleUsers WHERE Email = 'john@test.com';

-- Ver el plan Y ejecutar (más información)
EXPLAIN ANALYZE SELECT * FROM dbo.ExampleUsers WHERE Email = 'john@test.com';
```

**Salida de EXPLAIN ANALYZE:**

```
Index Scan using ix_example_users_email on "ExampleUsers"  (cost=0.42..8.44 rows=1 width=120)
  (actual time=0.042..0.044 rows=1 loops=1)
  Index Cond: ((email)::text = 'john@test.com'::text)
Planning Time: 0.1 ms
Execution Time: 0.1 ms
```

```
Seq Scan on "ExampleUsers"  (cost=0.00..4582.00 rows=1 width=120)
  (actual time=0.021..45.123 rows=1 loops=1)
  Filter: ((email)::text = 'john@test.com'::text)
  Rows Removed by Filter: 149999
Planning Time: 0.2 ms
Execution Time: 45.2 ms
```

**Nodos clave a identificar:**

| Nodo | Qué significa |
|------|---------------|
| `Index Scan` | Usa el índice — generalmente bueno |
| `Index Only Scan` | Solo lee el índice, no la tabla — muy eficiente |
| `Seq Scan` | Lee toda la tabla — problema si la tabla es grande |
| `Hash Join` | Join con hash table en memoria — bueno para tablas medianas |
| `Nested Loop` | Join fila por fila — eficiente con pocos datos, caro con muchos |
| `Sort` | Ordenamiento en memoria o disco — posible candidato para índice |

**cost=inicio..total:** tiempo relativo estimado. No son ms ni segundos. Son unidades internas. Lo importante es la magnitud relativa (10 vs 10000).

---

## Cuándo crear un índice

```
✓ Columnas que aparecen frecuentemente en WHERE
✓ Columnas usadas en JOIN (FK siempre deben tener índice)
✓ Columnas usadas en ORDER BY de queries paginados
✓ Columnas con restricción UNIQUE

✗ Tablas con muy pocas filas (< 1000) — seq scan es igual o más rápido
✗ Columnas con muy baja cardinalidad (ej: una columna booleana en tabla de 10M filas)
✗ Demasiados índices en tablas con muchos INSERT/UPDATE — cada índice ralentiza escrituras
```

---

## Relación con back-template

Los índices van en los archivos `NNN+1_<tabla>_indexes.sql`:

```sql
-- Host/Services/Schema Migration/Tables/002_example_users_indexes.sql

-- Búsqueda por PublicId (el ID que se expone al cliente)
CREATE INDEX IF NOT EXISTS ix_example_users_publicid
    ON dbo.ExampleUsers (PublicId);

-- Búsqueda por email (login, verificación de unicidad)
CREATE UNIQUE INDEX IF NOT EXISTS uix_example_users_email
    ON dbo.ExampleUsers (Email)
    WHERE DeletedAt IS NULL;

-- Solo usuarios activos — la mayoría de queries tienen este filtro
CREATE INDEX IF NOT EXISTS ix_example_users_active
    ON dbo.ExampleUsers (PublicId)
    WHERE DeletedAt IS NULL;
```

Checklist antes de entregar un nuevo módulo:
- ¿La FK tiene índice?
- ¿Las columnas de búsqueda frecuente tienen índice?
- ¿El índice es parcial si la mayoría de queries filtra `DeletedAt IS NULL`?
- ¿Se ejecutó `EXPLAIN ANALYZE` en los queries más críticos?

---

## Glosario

| Término | Definición |
|---------|-----------|
| Índice | estructura de datos auxiliar que acelera la búsqueda de filas en una tabla a cambio de espacio en disco y tiempo de escritura |
| B-tree | estructura de árbol balanceado usada por defecto en PostgreSQL para índices; eficiente para igualdad y rango |
| Índice compuesto | índice sobre dos o más columnas; solo es eficiente cuando las consultas filtran por las columnas del índice en el mismo orden |
| Índice parcial | índice que solo incluye filas que cumplen una condición (ej. `WHERE DeletedAt IS NULL`), reduciendo su tamaño |
| Índice de cobertura | índice que contiene todas las columnas necesarias para resolver una consulta, evitando acceder a la tabla principal |
| EXPLAIN ANALYZE | comando de PostgreSQL que muestra el plan de ejecución real de una consulta con tiempos medidos |
| Sequential Scan | modo de acceso que lee toda la tabla fila a fila; indica ausencia de índice útil para esa consulta |
| Index Scan | modo de acceso que usa el índice para localizar directamente las filas que cumplen la condición |
| Índice único | índice que garantiza la unicidad de los valores en una columna o combinación de columnas |
| Cardinalidad | número de valores distintos en una columna; columnas con alta cardinalidad se benefician más de un índice B-tree |
| Soft delete | patrón que marca registros con `DeletedAt` en lugar de borrarlos físicamente; requiere índices parciales para eficiencia |

---

*Rogelio Arriaga Gonzalez*
