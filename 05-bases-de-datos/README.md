# SQL — Índice

SQL y PostgreSQL para desarrollo backend: desde consultas básicas hasta optimización avanzada.

📁 Carpeta: [`sql/`](sql/)

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [Consultas](sql/01-consultas.md) | SELECT, WHERE, JOIN (INNER/LEFT), GROUP BY, ORDER BY, LIMIT/OFFSET, subqueries, EXISTS |
| 2 | [Índices](sql/02-indices.md) | B-tree, UNIQUE, parcial, compuesto, EXPLAIN ANALYZE, tipos de índice, cuándo crear |
| 3 | [Transacciones](sql/03-transacciones.md) | ACID, BEGIN/COMMIT/ROLLBACK, SAVEPOINT, isolation levels, deadlocks, Dapper |
| 4 | [PostgreSQL Avanzado](sql/04-postgresql-avanzado.md) | CTEs recursivas, window functions (ROW_NUMBER, LAG), upsert, JSONB, VACUUM |
| 5 | [PostgreSQL + Dapper](sql/05-postgresql-dapper.md) | Tipos C# ↔ PostgreSQL, convenciones del proyecto, mapeo, QueryMultiple |

---

## Mapa de relación con el proyecto

| Documento | Dónde se aplica en el proyecto |
|-----------|-------------------------------|
| Consultas | `...Sql` classes — QueryAsync, ExecuteScalarAsync con filtros y paginación |
| Índices | `NNN+1_tabla_indexes.sql` — índices parciales en `DeletedAt IS NULL` |
| Transacciones | Repositorios con operaciones multi-tabla — `BeginTransactionAsync` |
| PostgreSQL Avanzado | CTEs en queries complejos, `ON CONFLICT` en upserts, funciones de fecha |
| PostgreSQL + Dapper | `MainDapperDbConnection`, convenciones del esquema `dbo`, tipos de datos |

---

## Orden de lectura sugerido

1. [Consultas](sql/01-consultas.md) — la base
2. [Transacciones](sql/03-transacciones.md) — atomicidad en operaciones multi-tabla
3. [Índices](sql/02-indices.md) — optimización, EXPLAIN ANALYZE
4. [PostgreSQL Avanzado](sql/04-postgresql-avanzado.md) — CTEs, window functions, upsert
5. [PostgreSQL + Dapper](sql/05-postgresql-dapper.md) — cómo se integra todo en el proyecto


---

*Rogelio Arriaga Gonzalez*
