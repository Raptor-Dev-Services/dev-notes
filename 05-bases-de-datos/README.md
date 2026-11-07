# Bases de datos - Índice

SQL y PostgreSQL para desarrollo backend: desde consultas básicas hasta aislamiento de datos por tenant.

---

## Documentos

| # | Documento | Temas cubiertos |
|---|-----------|-----------------|
| 1 | [SQL Fundamentos](01-sql-fundamentos.md) | SELECT, JOINs, CTEs, window functions, ACID, índices |
| 2 | [Índices](02-indices.md) | B-tree, UNIQUE, parcial, compuesto, EXPLAIN ANALYZE, tipos de índice, cuándo crear |
| 3 | [Transacciones](03-transacciones.md) | ACID, BEGIN/COMMIT/ROLLBACK, SAVEPOINT, isolation levels, deadlocks, Dapper |
| 4 | [PostgreSQL Avanzado](04-postgresql-avanzado.md) | CTEs recursivas, window functions (ROW_NUMBER, LAG), upsert, JSONB, VACUUM |
| 5 | [Dapper Queries](05-dapper-queries.md) | QueryMultiple, multi-mapping, DynamicParameters, paginación, tipos C# y PostgreSQL |
| 6 | [Connection Strings](06-connection-strings.md) | Formato, pooling, resiliencia |
| 7 | [Migrations Multi-Tenant](07-migrations-multi-tenant.md) | Estrategias de migración por tenant |
| 8 | [Row Level Security](08-row-level-security.md) | RLS en PostgreSQL: políticas, bypass, performance |

---

## Mapa de relación con el proyecto

| Documento | Dónde se aplica en el proyecto |
|-----------|-------------------------------|
| SQL Fundamentos | `...Sql` classes: QueryAsync, ExecuteScalarAsync con filtros y paginación |
| Índices | `NNN+1_tabla_indexes.sql`: índices parciales en `DeletedAt IS NULL` |
| Transacciones | Repositorios con operaciones multi-tabla: `BeginTransactionAsync` |
| PostgreSQL Avanzado | CTEs en queries complejos, `ON CONFLICT` en upserts, funciones de fecha |
| Dapper Queries | `MainDapperDbConnection`, convenciones del esquema `dbo`, tipos de datos |

---

## Orden de lectura sugerido

1. [SQL Fundamentos](01-sql-fundamentos.md): la base
2. [Transacciones](03-transacciones.md): atomicidad en operaciones multi-tabla
3. [Índices](02-indices.md): optimización, EXPLAIN ANALYZE
4. [PostgreSQL Avanzado](04-postgresql-avanzado.md): CTEs, window functions, upsert
5. [Dapper Queries](05-dapper-queries.md): cómo se integra todo en el proyecto
6. [Connection Strings](06-connection-strings.md): cómo se conecta la aplicación
7. [Migrations Multi-Tenant](07-migrations-multi-tenant.md): cambios de esquema con varios tenants
8. [Row Level Security](08-row-level-security.md): aislamiento de datos en la propia base


---

*Rogelio Arriaga Gonzalez*
