# 08 — Row-Level Security en PostgreSQL

Row-Level Security (RLS) es una característica nativa de PostgreSQL que restringe las filas que devuelven o modifican los queries, directamente en el motor de base de datos — sin depender del código de la aplicación. Es una segunda línea de defensa para el aislamiento multi-tenant.

---

## Cómo funciona

```sql
-- Sin RLS:
SELECT * FROM dbo.user_profiles;
-- → devuelve TODAS las filas de todos los tenants

-- Con RLS y current_setting:
SELECT * FROM dbo.user_profiles;
-- → devuelve solo las filas donde tenant_id = current_setting('app.current_tenant_id')
-- La política se aplica automáticamente, incluso con SELECT * sin WHERE
```

---

## Configurar RLS en una tabla

```sql
-- 1. Habilitar RLS en la tabla
ALTER TABLE dbo.user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE dbo.user_profiles FORCE ROW LEVEL SECURITY;
-- FORCE aplica la política incluso al dueño de la tabla

-- 2. Crear la política de SELECT (lectura)
CREATE POLICY tenant_isolation_policy ON dbo.user_profiles
    FOR ALL                                               -- aplica a SELECT, INSERT, UPDATE, DELETE
    USING (tenant_id = current_setting('app.current_tenant_id')::BIGINT);

-- Para INSERT, la política USING se aplica al WHERE de la fila devuelta
-- Para INSERT, usar WITH CHECK para verificar los valores insertados:
CREATE POLICY tenant_insert_policy ON dbo.user_profiles
    FOR INSERT
    WITH CHECK (tenant_id = current_setting('app.current_tenant_id')::BIGINT);
```

---

## Pasar el tenant_id a PostgreSQL

El `current_setting` es una variable de sesión de PostgreSQL. Hay que establecerla en cada conexión:

### Opción A — En cada query (Dapper)

```csharp
await connection.ExecuteAsync(
    "SET LOCAL app.current_tenant_id = @tenantId",
    new { tenantId });

var users = await connection.QueryAsync<UserProfile>(
    "SELECT * FROM dbo.user_profiles");   // RLS filtra por tenant automáticamente
```

`SET LOCAL` aplica solo a la transacción actual. `SET` aplica a toda la sesión (peligroso con connection pooling).

### Opción B — Interceptor EF Core (automático)

```csharp
// Shared/Database/Interceptors/TenantRlsInterceptor.cs
public sealed class TenantRlsInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContextAccessor _accessor;

    public TenantRlsInterceptor(ITenantContextAccessor accessor) => _accessor = accessor;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct = default)
    {
        var tenantId = long.TryParse(_accessor.Current?.TenantId, out var id) ? id : 0L;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.current_tenant_id = '{tenantId}'";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

```csharp
// Shared/Database/ServiceCollectionEx.cs
services.AddSingleton<TenantRlsInterceptor>();

services.AddDbContext<AppDbContext>((sp, options) =>
    options
        .UseNpgsql(configuration.GetConnectionString("MainDbConnection"))
        .AddInterceptors(sp.GetRequiredService<TenantRlsInterceptor>()));
```

---

## RLS con tenant + branch

Para sistemas con dos niveles, RLS puede filtrar por ambos:

```sql
-- Política solo-tenant (tabla con solo tenant_id)
CREATE POLICY tenant_policy ON dbo.user_profiles
    FOR ALL
    USING (tenant_id = current_setting('app.current_tenant_id')::BIGINT);

-- Política tenant + branch (tabla con branch_id)
CREATE POLICY tenant_branch_policy ON dbo.production_orders
    FOR ALL
    USING (
        tenant_id = current_setting('app.current_tenant_id')::BIGINT
        AND (
            current_setting('app.current_branch_id', true) IS NULL   -- admin sin branch
            OR branch_id = current_setting('app.current_branch_id')::BIGINT
        )
    );
```

`current_setting('key', true)` con el segundo argumento `true` devuelve NULL en lugar de lanzar excepción si la variable no existe.

---

## Usuario de DB con privilegios limitados

Para que RLS sea efectivo, la conexión de la aplicación no debe ser un superuser:

```sql
-- Crear usuario de la app con permisos limitados
CREATE ROLE app_user WITH LOGIN PASSWORD 'app_password';

-- Dar permisos en las tablas (no en el schema directamente)
GRANT USAGE ON SCHEMA dbo TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA dbo TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA dbo TO app_user;

-- El superuser puede BYPASSRLS — la app no debe ser superuser
-- Si la app fuera superuser, FORCE ROW LEVEL SECURITY aplica igualmente
```

```
# Connection string de la app (appsettings.json)
Host=localhost;Database=back_template;Username=app_user;Password=app_password
# NO usar postgres o admin — solo los permisos necesarios
```

---

## RLS como defensa en profundidad

RLS no reemplaza los Global Query Filters de EF Core — los complementa:

```
Capa 1: Global Query Filter de EF Core    → filtra a nivel ORM (código)
Capa 2: Row-Level Security de PostgreSQL  → filtra a nivel DB (motor)

Si hay un bug en EF Core que omite el filtro:
→ RLS sigue bloqueando el acceso a filas de otro tenant
→ La app recibe 0 filas en lugar de datos de otro tenant
```

La estrategia mixta es el enfoque más robusto para SaaS de producción.

---

## Bypass de RLS para el superadmin del SaaS

El operador del SaaS necesita acceso sin restricciones para soporte y diagnóstico:

```sql
-- Crear rol de admin del SaaS que bypasa RLS
CREATE ROLE saas_admin WITH LOGIN PASSWORD 'admin_password' BYPASSRLS;

-- O temporalmente en una sesión:
SET LOCAL row_security = OFF;
SELECT * FROM dbo.user_profiles;   -- ve todos los tenants
SET LOCAL row_security = ON;
```

En el código, el SuperAdmin usa una conexión separada con el rol `saas_admin`:

```csharp
// Solo en el módulo de administración del SaaS
public sealed class SuperAdminRepository
{
    private readonly string _adminConnectionString;

    public async Task<List<UserProfile>> GetAllTenantsProfilesAsync(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        return (await connection.QueryAsync<UserProfile>(
            "SELECT * FROM dbo.user_profiles ORDER BY tenant_id, id")).ToList();
    }
}
```

---

## Verificar que RLS está funcionando

```sql
-- Ver las políticas activas
SELECT polname, polcmd, polqual
FROM pg_policies
WHERE tablename = 'user_profiles' AND schemaname = 'dbo';

-- Probar con tenant_id = 1 (debe ver solo filas del tenant 1)
SET app.current_tenant_id = '1';
SELECT COUNT(*) FROM dbo.user_profiles;   -- solo tenant 1

-- Probar con tenant_id = 2
SET app.current_tenant_id = '2';
SELECT COUNT(*) FROM dbo.user_profiles;   -- solo tenant 2

-- Sin tenant configurado (policy falla gracefully)
RESET app.current_tenant_id;
SELECT COUNT(*) FROM dbo.user_profiles;   -- 0 filas (current_setting retorna NULL → 0 == NULL es false)
```

---

## Rendimiento de RLS

Las políticas RLS agregan una condición WHERE implícita — el impacto en rendimiento es mínimo si los índices son correctos:

```sql
-- Índice que soporta el filtro RLS
CREATE INDEX ix_user_profiles_tenant ON dbo.user_profiles (tenant_id);
CREATE INDEX ix_production_orders_tenant_branch ON dbo.production_orders (tenant_id, branch_id);

-- EXPLAIN ANALYZE para verificar que usa el índice con RLS activo
SET app.current_tenant_id = '1';
EXPLAIN ANALYZE SELECT * FROM dbo.user_profiles WHERE is_active = true;
-- Debe mostrar Index Scan usando ix_user_profiles_tenant, no Seq Scan
```

---

## Migración para agregar RLS a una tabla existente

```sql
-- Paso 1: Agregar la política sin FORCE (para no romper código existente inmediatamente)
ALTER TABLE dbo.production_orders ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_branch_policy ON dbo.production_orders
    FOR ALL
    USING (
        tenant_id = current_setting('app.current_tenant_id', true)::BIGINT
    );

-- Paso 2: Actualizar la app para establecer current_tenant_id en cada conexión

-- Paso 3: Agregar FORCE una vez que la app esté actualizada
ALTER TABLE dbo.production_orders FORCE ROW LEVEL SECURITY;
```

---

## Limitaciones de RLS

| Limitación | Consecuencia |
|-----------|-------------|
| Connection pooling: `SET` persiste entre requests si no se usa `SET LOCAL` | Usar `SET LOCAL` dentro de una transacción, o el interceptor en `ConnectionOpened` |
| Superuser bypasa RLS por defecto | Usar `FORCE ROW LEVEL SECURITY` o usuario de app sin SUPERUSER |
| No filtra funciones con `SECURITY DEFINER` | Evitar funciones con SECURITY DEFINER que accedan a tablas con RLS |
| Complejidad operacional | Documentar bien qué tablas tienen RLS y cómo se establece el setting |

---

## Checklist

- [ ] `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` en tablas multi-tenant
- [ ] Política `FOR ALL` con `USING (tenant_id = current_setting(...)::BIGINT)`
- [ ] Usuario de la app no es superuser (`BYPASSRLS` no activo)
- [ ] `SET LOCAL` (no `SET`) para establecer el tenant_id — evita fugas entre conexiones
- [ ] Interceptor EF Core establece el setting al abrir la conexión
- [ ] Índices en `(tenant_id)` y `(tenant_id, branch_id)` para rendimiento
- [ ] Verificar con `EXPLAIN ANALYZE` que no hay Seq Scan por RLS

---

## Glosario

| Término | Definición |
|---------|-----------|
| Row Level Security (RLS) | mecanismo de PostgreSQL que filtra automáticamente las filas a nivel de motor según políticas definidas por tabla |
| Política RLS | regla declarada con `CREATE POLICY` que define qué filas son visibles o modificables para una sesión |
| FORCE ROW LEVEL SECURITY | modificador que aplica RLS incluso al propietario de la tabla, evitando bypass accidental por permisos elevados |
| SET LOCAL | variante de `SET` que aplica la configuración solo durante la transacción actual; evita fugas entre conexiones del pool |
| current_setting | función de PostgreSQL que lee una variable de sesión (`app.current_tenant_id`) usada por las políticas RLS |
| app.current_tenant_id | variable de sesión personalizada que establece el contexto del tenant activo en cada conexión |
| BYPASSRLS | privilegio de PostgreSQL que permite a un rol ignorar todas las políticas RLS — nunca conceder al usuario de la app |
| SECURITY DEFINER | función que se ejecuta con los privilegios del dueño de la función, no del llamador — puede bypassar RLS |
| EF Core Interceptor | clase que intercepta eventos del ciclo de vida de EF Core (apertura de conexión, guardado) para ejecutar código transversal |
| Fuga de datos entre tenants | acceso a datos de otro tenant debido a ausencia o mala configuración de aislamiento en multi-tenancy |

---

*Rogelio Arriaga Gonzalez*
