# 07 — Migrations en Multi-Tenant

Aplicar cambios de esquema en un SaaS con tenants reales en producción es diferente a un sistema de un solo tenant. Las migraciones deben ser aditivas, ejecutarse sin downtime y no romper la aplicación mientras se aplican.

---

## El problema

En un sistema multi-tenant con cientos de tenants, una migración que:
- Bloquea una tabla durante minutos → todos los tenants experimentan downtime
- Elimina una columna que el código anterior todavía lee → crash inmediato
- Renombra una columna sin backwards compatibility → incompatibilidad código-esquema

```
PELIGROSO:
ALTER TABLE dbo.user_profiles RENAME COLUMN full_name TO display_name;
→ La versión anterior del código busca "full_name" → 500 en todos los tenants
```

---

## El principio: cambios aditivos

**Solo agregar, nunca eliminar ni renombrar en un solo paso.**

```
✓ ADD COLUMN
✓ ADD TABLE
✓ ADD INDEX (CONCURRENTLY en PostgreSQL)
✓ ADD NULLABLE COLUMN
✓ ADD FOREIGN KEY DEFERRABLE

❌ DROP COLUMN       → primero eliminar del código, luego de la DB
❌ RENAME COLUMN     → agregar nueva columna, migrar datos, eliminar la vieja
❌ DROP TABLE        → idem
❌ ADD NOT NULL sin DEFAULT  → bloquea la tabla durante el backfill
```

---

## Estrategia con EF Core Migrations

### Generar una migración

```bash
# Siempre desde la raíz del repo
dotnet ef migrations add NombreDeMigracion \
  --project back-template/Shared/Database \
  --startup-project back-template/Host.Api \
  --output-dir Migrations

# Revisar el SQL generado ANTES de aplicar
dotnet ef migrations script \
  --project back-template/Shared/Database \
  --startup-project back-template/Host.Api \
  --output migration_preview.sql
```

### Revisar el SQL siempre

El SQL generado por EF Core puede no ser óptimo. Siempre revisarlo:

```csharp
// Una migración generada por EF Core para agregar columna:
migrationBuilder.AddColumn<string>(
    name: "Phone",
    table: "user_profiles",
    schema: "dbo",
    maxLength: 20,
    nullable: true);   // ← NULLABLE es seguro — no bloquea
```

---

## Agregar columna NOT NULL — patrón seguro

Agregar una columna `NOT NULL` directamente en producción bloquea la tabla para el backfill. El patrón seguro es en dos fases:

### Fase 1 (deploy actual)

```csharp
// Migración: agregar como NULLABLE primero
migrationBuilder.AddColumn<string>(
    name: "PhoneNumber",
    table: "user_profiles",
    schema: "dbo",
    maxLength: 20,
    nullable: true);   // ← nullable en fase 1

// Backfill con valor default (sin bloqueo de tabla en PostgreSQL)
migrationBuilder.Sql("""
    UPDATE dbo.user_profiles SET phone_number = '' WHERE phone_number IS NULL;
    """);
```

### Fase 2 (siguiente deploy, cuando todos los registros tienen valor)

```csharp
// Segunda migración: hacer NOT NULL con un DEFAULT existente
migrationBuilder.AlterColumn<string>(
    name: "PhoneNumber",
    table: "user_profiles",
    schema: "dbo",
    nullable: false,
    defaultValue: "",
    oldNullable: true);
```

---

## Renombrar columna — patrón seguro en 3 fases

### Fase 1 — agregar columna nueva, mantener la vieja

```sql
ALTER TABLE dbo.user_profiles ADD COLUMN display_name VARCHAR(200);
UPDATE dbo.user_profiles SET display_name = full_name;
```

Código en esta fase: escribe en AMBAS columnas, lee de `full_name` (la vieja).

### Fase 2 — código lee de la nueva columna

Deployer actualiza código para leer `display_name`. Todavía escribe en ambas durante el deploy rolling.

### Fase 3 — eliminar columna vieja

```sql
ALTER TABLE dbo.user_profiles DROP COLUMN full_name;
```

Solo cuando ya no hay ninguna instancia del código viejo corriendo.

---

## PostgreSQL CONCURRENTLY — índices sin bloqueo

EF Core genera `CREATE INDEX` sin `CONCURRENTLY`. En tablas grandes, esto bloquea lecturas y escrituras. Siempre usar `CONCURRENTLY` para índices en producción:

```csharp
// En la migración — anular el SQL generado por EF Core
protected override void Up(MigrationBuilder migrationBuilder)
{
    // EF Core generaría: CREATE INDEX ix_... ON dbo.user_profiles (tenant_id, is_active)
    // Nosotros reemplazamos con CONCURRENTLY:
    migrationBuilder.Sql("""
        CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_user_profiles_tenant_active
        ON dbo.user_profiles (tenant_id, is_active)
        WHERE is_active = true;
        """);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP INDEX CONCURRENTLY IF EXISTS ix_user_profiles_tenant_active;");
}
```

**Importante:** `CONCURRENTLY` no puede ejecutarse dentro de una transacción. EF Core wrappea las migraciones en transacciones. Hay que desactivarlo:

```csharp
// Deshabilitar transacción para esta migración
protected override void BuildTargetModel(ModelBuilder modelBuilder) { }

// En el método Up/Down con migrationBuilder.Sql:
migrationBuilder.Sql("SET lock_timeout = '3s';");  // falla en lugar de bloquear
```

O mejor: ejecutar el `CREATE INDEX CONCURRENTLY` fuera de la migración, manualmente, antes del deploy.

---

## Database.Migrate() vs EnsureCreated()

```
EnsureCreated()  → crea el esquema desde cero si no existe
                   NO ejecuta migraciones
                   Útil solo en desarrollo y tests

Database.Migrate() → ejecuta todas las migraciones pendientes
                     Safe para producción
                     Registra cada migración en __EFMigrationsHistory
```

### Cuándo usar cada uno

| Entorno | Método |
|---------|--------|
| Development | `EnsureCreated()` — recrear rápido sin historial |
| Test | `EnsureCreated()` — DB limpia por test |
| Staging/Production | `Database.Migrate()` — incremental, con rollback |

### Cambiar DatabaseInitializationService para producción

```csharp
// Shared/Database/ServiceCollectionEx.cs
internal sealed class DatabaseInitializationService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        accessor.Current = new TenantContext("0");   // dummy para filtros

        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (env.IsDevelopment())
            await db.Database.EnsureCreatedAsync(ct);
        else
            await db.Database.MigrateAsync(ct);   // producción → migrations
    }
}
```

---

## Zero-downtime migrations en producción

### Estrategia Blue-Green

```
1. DB en estado N (esquema actual)
2. Aplicar migración aditiva a la DB → estado N+1 (backwards compatible)
3. Deploy de la nueva versión del código (funciona con esquema N y N+1)
4. Si hay problemas → rollback del código, la DB sigue en N+1 (sin efecto)
5. Cuando el deploy está estable → limpiar columnas/tablas ya no usadas
```

### Verificación antes del deploy

```bash
# Ver las migraciones pendientes sin aplicarlas
dotnet ef migrations list \
  --project back-template/Shared/Database \
  --startup-project back-template/Host.Api

# Generar el script SQL de las migraciones pendientes
dotnet ef migrations script --from LastApplied \
  --project back-template/Shared/Database \
  --startup-project back-template/Host.Api \
  --output pending_migrations.sql

# Revisar el SQL antes de aplicar
cat pending_migrations.sql
```

---

## Lock timeout — evitar bloqueos largos

Si una migración necesita un lock exclusivo, configurar un timeout para que falle rápido en lugar de bloquear:

```sql
-- Al inicio del script de migración en producción
SET lock_timeout = '5s';    -- falla si no puede obtener el lock en 5 segundos
SET statement_timeout = '60s';  -- falla si tarda más de 60 segundos
```

Si falla con timeout: diagnosticar qué query tiene el lock, matarla si es seguro, reintentar.

---

## Tabla __EFMigrationsHistory

EF Core mantiene un registro de las migraciones aplicadas:

```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY applied;
-- MigrationId                            | ProductVersion
-- 20250101120000_InitialCreate           | 10.0.0
-- 20250215093000_AddPhoneToUserProfile   | 10.0.0
-- 20250301160000_AddBranchesToTenant     | 10.0.0
```

Si necesitas "saltar" una migración problemática (ya fue aplicada manualmente):

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250301160000_AddBranchesToTenant', '10.0.0');
```

---

## Migraciones de datos (seed/backfill)

Para migrar datos entre columnas o poblar datos iniciales:

```csharp
// Separar migración de esquema de migración de datos
// Migración de esquema: AddColumn
// Migración de datos: Update con lógica de negocio

protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Agregar columna slug vacía
    migrationBuilder.AddColumn<string>(
        name: "Slug", table: "tenants", schema: "dbo",
        maxLength: 50, nullable: true);

    // 2. Backfill: generar slug desde name (lógica SQL básica)
    migrationBuilder.Sql("""
        UPDATE dbo.tenants
        SET slug = LOWER(REGEXP_REPLACE(name, '[^a-zA-Z0-9]', '-', 'g'))
        WHERE slug IS NULL;
        """);

    // 3. Hacer NOT NULL después del backfill
    migrationBuilder.AlterColumn<string>(
        name: "Slug", table: "tenants", schema: "dbo",
        nullable: false, oldNullable: true);

    // 4. Índice único
    migrationBuilder.CreateIndex(
        name: "ix_tenants_slug",
        table: "tenants", schema: "dbo",
        column: "Slug", unique: true);
}
```

---

## Checklist de migración en producción

- [ ] Revisar el SQL generado (`dotnet ef migrations script`)
- [ ] Las columnas nuevas son `nullable` o tienen `DEFAULT` — no bloquean
- [ ] Índices nuevos usan `CONCURRENTLY` — no bloquean lecturas
- [ ] `lock_timeout` configurado para evitar bloqueos largos
- [ ] La migración es backwards-compatible con el código anterior
- [ ] Probar la migración en staging antes de producción
- [ ] Tener plan de rollback (¿qué `Down` ejecutaría la migración?)
- [ ] Backfill de datos separado del cambio de esquema si es grande
- [ ] Verificar en `__EFMigrationsHistory` después del deploy

---

## Glosario

| Término | Definición |
|---------|-----------|
| Migración | cambio versionado de esquema de base de datos que EF Core puede aplicar y revertir de forma controlada |
| Migración aditiva | migración que solo agrega columnas, tablas o índices sin modificar los existentes — compatible con el código anterior |
| Breaking migration | migración que elimina o renombra columnas usadas por el código anterior, requiriendo coordinación con el deploy |
| Backfill | migración de datos que rellena columnas nuevas con valores derivados de datos existentes |
| __EFMigrationsHistory | tabla que EF Core mantiene en la base de datos con el registro de las migraciones aplicadas |
| Zero-downtime migration | estrategia de tres fases (agregar, migrar datos, eliminar lo viejo) que permite cambios de esquema sin interrumpir el servicio |
| CONCURRENTLY | cláusula de PostgreSQL para crear índices sin bloquear lecturas ni escrituras mientras se construye el índice |
| lock_timeout | configuración de PostgreSQL que cancela una operación si no puede adquirir el lock requerido en el tiempo especificado |
| Global Query Filter | filtro aplicado automáticamente por EF Core a todas las queries de una entidad (ej. `WHERE tenant_id = @current`) |
| ITenantContextAccessor | interfaz que provee el `TenantId` del request actual para usarlo en filtros de consulta multi-tenant |

---

*Rogelio Arriaga Gonzalez*
