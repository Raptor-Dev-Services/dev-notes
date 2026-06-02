# 03 — Transacciones: ACID, Isolation Levels y Deadlocks

Una transacción agrupa múltiples operaciones en una unidad atómica: todo se aplica o nada se aplica.

---

## ACID — las 4 propiedades

| Propiedad | Qué garantiza |
|-----------|--------------|
| **Atomicidad** | Las operaciones son una unidad — si una falla, todas se revierten |
| **Consistencia** | La DB pasa de un estado válido a otro estado válido — nunca a un estado inválido |
| **Aislamiento** | Transacciones concurrentes no se ven entre sí (hasta que hacen COMMIT) |
| **Durabilidad** | Los datos commiteados sobreviven a reinicios del servidor |

---

## BEGIN / COMMIT / ROLLBACK

```sql
-- Iniciar una transacción
BEGIN;

-- Operaciones atómicas — o se aplican todas o ninguna
UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE Id = 1;  -- débito
UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE Id = 2;  -- crédito

-- Confirmar los cambios (los hace visibles y permanentes)
COMMIT;

-- O revertir si algo falla
ROLLBACK;
```

```sql
-- Ejemplo con manejo de error
BEGIN;

UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE Id = 1;

-- Verificar que la cuenta no quedó negativa
DO $$
BEGIN
    IF (SELECT Balance FROM dbo.Accounts WHERE Id = 1) < 0 THEN
        RAISE EXCEPTION 'Saldo insuficiente';
    END IF;
END $$;

UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE Id = 2;

COMMIT;
-- Si RAISE EXCEPTION ocurrió, PostgreSQL revierte automáticamente
```

---

## Transacciones en Dapper

```csharp
// Infrastructure/Repositories/AccountRepository.cs
public async Task TransferAsync(
    Guid fromAccountId, Guid toAccountId, decimal amount, CancellationToken ct)
{
    await using var conn        = await _factory.OpenConnectionAsync(ct);
    await using var transaction = await conn.BeginTransactionAsync(ct);

    try
    {
        // Débito
        await conn.ExecuteAsync(
            "UPDATE dbo.Accounts SET Balance = Balance - @amount WHERE PublicId = @id;",
            new { amount, id = fromAccountId },
            transaction: transaction);

        // Verificar saldo
        var balance = await conn.ExecuteScalarAsync<decimal>(
            "SELECT Balance FROM dbo.Accounts WHERE PublicId = @id;",
            new { id = fromAccountId },
            transaction: transaction);

        if (balance < 0)
            throw new InvalidOperationException("Saldo insuficiente.");

        // Crédito
        await conn.ExecuteAsync(
            "UPDATE dbo.Accounts SET Balance = Balance + @amount WHERE PublicId = @id;",
            new { amount, id = toAccountId },
            transaction: transaction);

        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;  // re-lanzar para que el GlobalExceptionHandler lo maneje
    }
}
```

**Patrón en back-template:** `MainDapperDbConnection` no expone transacciones directamente — las operaciones multi-tabla que requieren transacción se manejan en el repositorio con `NpgsqlConnection` y `NpgsqlTransaction` directas.

---

## SAVEPOINT — puntos de retorno parcial

```sql
BEGIN;

INSERT INTO dbo.Orders (UserId, Total) VALUES (1, 500);

SAVEPOINT before_items;  -- punto de retorno

INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity) VALUES (1, 99, 2);

-- Si el item falla, revertir solo desde el savepoint (no toda la transacción)
ROLLBACK TO SAVEPOINT before_items;

-- La orden sigue intacta, solo se revirtió el item
COMMIT;
```

---

## Isolation Levels

Controlan cuánto aislamiento tienen las transacciones concurrentes. Mayor aislamiento = menor rendimiento.

| Nivel | Dirty Read | Non-repeatable Read | Phantom Read |
|-------|-----------|---------------------|--------------|
| `READ UNCOMMITTED` | Posible | Posible | Posible |
| `READ COMMITTED` (default PostgreSQL) | No | Posible | Posible |
| `REPEATABLE READ` | No | No | Posible |
| `SERIALIZABLE` | No | No | No |

### Problemas de concurrencia

**Dirty Read:** leer datos que otra transacción modificó pero no commiteó aún.
```sql
-- T1: UPDATE balance = 0 (sin commit)
-- T2: SELECT balance → lee 0 (datos sucios)
-- T1: ROLLBACK → el balance vuelve al valor original, T2 leyó algo que nunca existió
```

**Non-repeatable Read:** dos lecturas de la misma fila en la misma transacción dan resultados diferentes.
```sql
-- T1: SELECT balance → 1000
-- T2: UPDATE balance = 500; COMMIT
-- T1: SELECT balance → 500 (cambió!)
```

**Phantom Read:** una query devuelve filas diferentes en dos ejecuciones dentro de la misma transacción.
```sql
-- T1: SELECT COUNT(*) WHERE IsActive = true → 100
-- T2: INSERT nuevo usuario activo; COMMIT
-- T1: SELECT COUNT(*) WHERE IsActive = true → 101 (apareció un "fantasma")
```

### Cambiar el isolation level

```sql
-- Para una transacción específica
BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ;

-- Para una sesión
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
```

```csharp
// En Dapper / Npgsql
var transaction = await conn.BeginTransactionAsync(
    IsolationLevel.RepeatableRead, ct);
```

**En la práctica:** `READ COMMITTED` (el default) es suficiente para la mayoría de aplicaciones web. `REPEATABLE READ` o `SERIALIZABLE` se usan para operaciones financieras críticas.

---

## Deadlocks

Un deadlock ocurre cuando dos transacciones se bloquean mutuamente esperando recursos que la otra tiene:

```
T1: LOCK A → espera B
T2: LOCK B → espera A
→ Ambas esperan para siempre → PostgreSQL detecta y mata una (la que es más fácil de reintentar)
```

```sql
-- T1:
BEGIN;
UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE Id = 1;  -- lock en fila 1
-- ... espera ...
UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE Id = 2;  -- quiere lock en fila 2 → DEADLOCK

-- T2 (concurrente):
BEGIN;
UPDATE dbo.Accounts SET Balance = Balance - 50  WHERE Id = 2;  -- lock en fila 2
UPDATE dbo.Accounts SET Balance = Balance + 50  WHERE Id = 1;  -- quiere lock en fila 1 → DEADLOCK
```

### Prevenir deadlocks

**1. Ordenar los locks consistentemente** — siempre adquirir locks en el mismo orden:

```sql
-- ✓ Siempre actualizar primero la cuenta con menor Id
-- T1 y T2 siempre adquieren locks en el mismo orden → no hay deadlock
UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE Id = 1;  -- menor Id primero
UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE Id = 2;
```

**2. Usar SELECT FOR UPDATE** para adquirir el lock antes de modificar:

```sql
BEGIN;
-- Adquirir locks en orden
SELECT Balance FROM dbo.Accounts WHERE Id IN (1, 2) ORDER BY Id FOR UPDATE;
-- Ahora hacer las actualizaciones
UPDATE dbo.Accounts SET Balance = Balance - 100 WHERE Id = 1;
UPDATE dbo.Accounts SET Balance = Balance + 100 WHERE Id = 2;
COMMIT;
```

**3. Mantener transacciones cortas** — menos tiempo con locks = menos probabilidad de conflicto.

---

## Relación con back-template

Las migraciones del proyecto son idempotentes (`CREATE TABLE IF NOT EXISTS`). Si fallan a mitad, se pueden re-ejecutar sin problema porque PostgreSQL revierte DDL automáticamente cuando hay un error.

Para operaciones multi-tabla en el proyecto:

```csharp
// Infrastructure/Repositories/ExampleUserRepository.cs
// Cuando se necesita atomicidad entre dos tablas
public async Task InsertWithRoleAsync(
    ExampleUser user, string role, CancellationToken ct)
{
    await using var conn  = await _factory.OpenConnectionAsync(ct);
    await using var tx    = await conn.BeginTransactionAsync(ct);
    try
    {
        await conn.ExecuteAsync(InsertUserSql, new { ... }, transaction: tx);
        await conn.ExecuteAsync(InsertRoleSql, new { ... }, transaction: tx);
        await tx.CommitAsync(ct);
    }
    catch { await tx.RollbackAsync(ct); throw; }
}
```

---

## Bloqueos optimistas vs pesimistas
> Fuente: *Procesamiento de Bases de Datos* (Kroenke) — Ch.11 Concurrencia y Recuperación

### Bloqueo pesimista (Pessimistic Locking)

Asume que habrá conflicto — bloquea el recurso antes de modificarlo.

```sql
-- SELECT FOR UPDATE — bloquea la fila hasta el COMMIT
BEGIN;
SELECT * FROM ExampleUsers WHERE PublicId = '...' FOR UPDATE;
-- Ninguna otra transacción puede modificar esta fila hasta que hagamos COMMIT
UPDATE ExampleUsers SET FullName = 'Nuevo Nombre' WHERE PublicId = '...';
COMMIT;
```

```csharp
// Dapper — SELECT FOR UPDATE
public async Task<ExampleUser?> GetForUpdateAsync(Guid publicId, IDbTransaction tx, CancellationToken ct)
    => await _db.QuerySingleOrDefaultAsync<ExampleUser>(
        "SELECT * FROM ExampleUsers WHERE PublicId = @publicId FOR UPDATE",
        new { publicId },
        transaction: tx);
```

**Usar cuando:**
- Conflictos son frecuentes (múltiples usuarios editando el mismo recurso)
- La operación es corta (bloquear por milisegundos, no segundos)
- Inventario en tiempo real, cuentas bancarias

### Bloqueo optimista (Optimistic Locking)

Asume que NO habrá conflicto — detecta el conflicto al guardar mediante un número de versión o timestamp.

```sql
-- La tabla tiene una columna de versión
ALTER TABLE ExampleUsers ADD COLUMN Version INT NOT NULL DEFAULT 1;

-- UPDATE solo si la versión no cambió
UPDATE ExampleUsers
SET    FullName = @FullName, Version = Version + 1
WHERE  PublicId = @PublicId
  AND  Version  = @ExpectedVersion;

-- Si afectó 0 filas → alguien más modificó el registro → conflicto
```

```csharp
// Handler con concurrencia optimista
public async Task<UpdateExampleUserResponse> Handle(
    UpdateExampleUserRequest request, CancellationToken ct)
{
    var rowsAffected = await _db.ExecuteAsync(
        """
        UPDATE ExampleUsers
        SET FullName = @FullName, Version = Version + 1, UpdatedAtUtc = NOW()
        WHERE PublicId = @PublicId AND Version = @ExpectedVersion
        """,
        new
        {
            request.FullName,
            request.PublicId,
            request.ExpectedVersion
        });

    if (rowsAffected == 0)
        return new UpdateExampleUserConflictFailure(
            "El registro fue modificado por otro usuario. Recargue y vuelva a intentar.");

    return new UpdateExampleUserSuccess();
}
```

| | Pesimista | Optimista |
|---|---|---|
| **Conflictos** | Frecuentes | Raros |
| **Operación** | Corta (ms) | Puede ser larga |
| **Penalización por conflicto** | Espera | Reintento |
| **Escala** | Peor bajo alta concurrencia | Mejor bajo alta concurrencia |
| **Casos de uso** | Inventario, cuentas, reservas | CMS, perfiles, configuraciones |

---

## Glosario

| Término | Definición |
|---------|-----------|
| Transacción | unidad de trabajo que cumple las propiedades ACID: se ejecuta toda o no se ejecuta nada |
| ACID | Atomicidad, Consistencia, Aislamiento y Durabilidad — propiedades que garantizan la integridad de una transacción |
| Aislamiento | propiedad que determina en qué medida una transacción es visible para otras transacciones concurrentes |
| Nivel de aislamiento | configuración que establece cuánta inconsistencia temporal permite una transacción a cambio de mayor concurrencia |
| Dirty read | lectura de datos modificados por una transacción no confirmada aún; ocurre en el nivel Read Uncommitted |
| Non-repeatable read | fenómeno donde una misma lectura dentro de una transacción retorna valores distintos porque otra transacción los modificó |
| Phantom read | fenómeno donde una consulta repetida dentro de una transacción retorna filas adicionales que otra transacción insertó |
| Deadlock | bloqueo mutuo donde dos transacciones esperan indefinidamente el lock que la otra tiene |
| Bloqueo pesimista | estrategia que adquiere el lock antes de modificar, asumiendo que habrá conflicto (`SELECT FOR UPDATE`) |
| Bloqueo optimista | estrategia que detecta el conflicto al guardar mediante un número de versión, sin bloquear filas |
| SELECT FOR UPDATE | cláusula SQL que adquiere un lock exclusivo sobre las filas seleccionadas hasta el siguiente COMMIT o ROLLBACK |
| Número de versión | columna entera que se incrementa en cada UPDATE, usada para detectar modificaciones concurrentes |

---

*Rogelio Arriaga Gonzalez*
