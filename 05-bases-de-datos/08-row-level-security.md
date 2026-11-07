# 08 — Row-Level Security en PostgreSQL

Row-Level Security (RLS) es una característica nativa de PostgreSQL que restringe las filas que devuelven o modifican los queries, directamente en el motor de base de datos, sin depender del código de la aplicación. Es una segunda línea de defensa para el aislamiento multi-tenant.

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
-- FORCE aplica la política al dueño de la tabla, que sin él se la salta.
-- No alcanza a un superusuario ni a un rol con BYPASSRLS: esos se la saltan siempre.

-- 2. Crear la política
CREATE POLICY tenant_isolation_policy ON dbo.user_profiles
    FOR ALL                                               -- aplica a SELECT, INSERT, UPDATE, DELETE
    USING      (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::BIGINT)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::BIGINT);
```

`USING` decide qué filas existentes se ven. `WITH CHECK` valida la fila que se quiere escribir. Si una política `FOR ALL` no trae `WITH CHECK`, PostgreSQL reutiliza el `USING`: un `INSERT` o un `UPDATE` hacia otro tenant falla con `new row violates row-level security policy`. Escribirlo explícito no cambia el resultado, pero se lee mejor, y hace innecesaria una segunda política `FOR INSERT`.

**Por qué `NULLIF` y el segundo argumento.** Sin ese `true`, `current_setting` lanza `unrecognized configuration parameter` si la variable nunca se fijó en la sesión. Con `true` devuelve NULL, pero solo hasta la primera vez que se usa: tras un `RESET`, o tras una transacción que la fijó con alcance local, la variable queda en cadena vacía, y `''::BIGINT` lanza `invalid input syntax for type bigint: ""`. `NULLIF(..., '')` convierte esa cadena en NULL: la comparación deja de ser verdadera y la consulta devuelve 0 filas en lugar de un error.

---

## Pasar el tenant_id a PostgreSQL

`current_setting` lee una variable de PostgreSQL. Hay que fijarla antes de consultar, y la forma de hacerlo con un valor que viene de la aplicación es `set_config`:

### Opción A — En cada query (Dapper)

```csharp
await using var tx = await connection.BeginTransactionAsync(ct);

await connection.ExecuteAsync(
    "SELECT set_config('app.current_tenant_id', @tenantId, true)",
    new { tenantId = tenantId.ToString() },   // set_config recibe texto
    tx);

var users = await connection.QueryAsync<UserProfile>(
    "SELECT * FROM dbo.user_profiles",        // RLS filtra por tenant automáticamente
    transaction: tx);

await tx.CommitAsync(ct);
```

`set_config(nombre, valor, true)` hace lo mismo que `SET LOCAL`, pero acepta parámetros: el `true` limita el valor a la transacción actual. `SET` no los acepta. `SET LOCAL app.current_tenant_id = @tenantId` falla con Npgsql con `syntax error at or near "$1"`.

Tres detalles que conviene conocer antes de que fallen:

- **Necesita transacción.** Sin ella, `set_config(..., true)` dura solo su propia sentencia y la consulta siguiente devuelve 0 filas. `SET LOCAL` fuera de una transacción tampoco hace nada: solo deja un `WARNING`.
- **El valor va como texto.** Con un `long`, PostgreSQL responde `function set_config(unknown, bigint, boolean) does not exist`.
- **Con `false` dura toda la sesión,** igual que `SET`. La opción B usa ese alcance, con las condiciones que explica.

### Opción B — Interceptor EF Core (automático)

```csharp
// Shared/Database/Interceptors/TenantRlsInterceptor.cs
public sealed class TenantRlsInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContextAccessor _accessor;

    public TenantRlsInterceptor(ITenantContextAccessor accessor) => _accessor = accessor;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = CrearComando(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct = default)
    {
        await using var cmd = CrearComando(connection);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private DbCommand CrearComando(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', @tenant, false)";

        var tenant = cmd.CreateParameter();
        tenant.ParameterName = "tenant";
        // Sin tenant va cadena vacía: la política queda en NULL y devuelve 0 filas
        tenant.Value = long.TryParse(_accessor.Current?.TenantId, out var id) ? id.ToString() : string.Empty;
        cmd.Parameters.Add(tenant);

        return cmd;
    }
}
```

El valor viaja como parámetro, nunca interpolado en el SQL. Hacen falta las dos sobrecargas: EF Core llama a `ConnectionOpened` en las consultas síncronas, y si solo existe la asíncrona la variable no se fija y esas consultas devuelven 0 filas sin ningún error.

Aquí el tercer argumento es `false` porque el interceptor corre al abrir la conexión, fuera de cualquier transacción, y el valor tiene que durar toda la sesión. Es seguro por dos razones: se vuelve a fijar en cada apertura, y Npgsql limpia la sesión al devolver la conexión al pool. Con `No Reset On Close=true` esa limpieza se apaga y el valor sobrevive a la apertura siguiente. Con un pool externo como PgBouncer en modo transacción, `SET` y sus equivalentes de sesión no se pueden usar: ahí toca la opción A.

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
    USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::BIGINT);

-- Política tenant + branch (tabla con branch_id)
CREATE POLICY tenant_branch_policy ON dbo.production_orders
    FOR ALL
    USING (
        tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::BIGINT
        AND (
            NULLIF(current_setting('app.current_branch_id', true), '') IS NULL   -- admin sin branch
            OR branch_id = NULLIF(current_setting('app.current_branch_id', true), '')::BIGINT
        )
    );
```

`current_setting('key', true)` con el segundo argumento `true` devuelve NULL en lugar de lanzar excepción si la variable no existe. Todas las lecturas llevan ese `true` y su `NULLIF`: basta con una lectura sin ellos para que el camino del admin sin branch falle con `unrecognized configuration parameter`, o con `invalid input syntax for type bigint: ""` si el branch se limpió antes.

---

## Usuario de DB con privilegios limitados

Para que RLS sea efectivo, la conexión de la aplicación no debe ser un superusuario ni un rol con `BYPASSRLS`. Si además es dueña de las tablas, esas tablas necesitan `FORCE`:

```sql
-- Crear usuario de la app con permisos limitados
CREATE ROLE app_user WITH LOGIN PASSWORD 'app_password';

-- Dar permisos en las tablas (no en el schema directamente)
GRANT USAGE ON SCHEMA dbo TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA dbo TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA dbo TO app_user;

-- Un superusuario o un rol con BYPASSRLS se salta RLS siempre: ni FORCE lo alcanza.
-- Por eso la app nunca se conecta con uno de esos roles.
```

```
# Connection string de la app (appsettings.json)
Host=localhost;Database=back_template;Username=app_user;Password=app_password
# NO usar postgres o admin — solo los permisos necesarios
```

---

## RLS como defensa en profundidad

RLS no reemplaza los Global Query Filters de EF Core. Los complementa:

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
-- Crear rol de admin del SaaS que se salta RLS
CREATE ROLE saas_admin WITH LOGIN PASSWORD 'admin_password' BYPASSRLS;
```

`SET row_security = OFF` no es un atajo para lo mismo: no apaga las políticas. Con esa variable en `off`, una consulta que la política filtraría falla con `query would be affected by row-level security policy for table "user_profiles"`. Sirve para que herramientas como `pg_dump` fallen en vez de sacar datos a medias.

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
SELECT policyname, cmd, qual, with_check
FROM pg_policies
WHERE tablename = 'user_profiles' AND schemaname = 'dbo';

-- Lo que sigue se prueba conectado como app_user: un superusuario vería todas las filas.

-- Probar con tenant_id = 1 (debe ver solo filas del tenant 1)
SET app.current_tenant_id = '1';
SELECT COUNT(*) FROM dbo.user_profiles;   -- solo tenant 1

-- Probar con tenant_id = 2
SET app.current_tenant_id = '2';
SELECT COUNT(*) FROM dbo.user_profiles;   -- solo tenant 2

-- Sin tenant configurado: RESET deja la variable vacía, NULLIF la vuelve NULL y no pasa ninguna fila
RESET app.current_tenant_id;
SELECT COUNT(*) FROM dbo.user_profiles;   -- 0 filas
```

---

## Rendimiento de RLS

Las políticas RLS agregan una condición WHERE implícita. El impacto en rendimiento es mínimo si los índices son correctos:

```sql
-- Índice que soporta el filtro RLS
CREATE INDEX ix_user_profiles_tenant ON dbo.user_profiles (tenant_id);
CREATE INDEX ix_production_orders_tenant_branch ON dbo.production_orders (tenant_id, branch_id);

-- EXPLAIN ANALYZE para verificar que usa el índice con RLS activo
SET app.current_tenant_id = '1';
EXPLAIN ANALYZE SELECT * FROM dbo.user_profiles WHERE is_active = true;
-- Con volumen real debe usar ix_user_profiles_tenant (Index Scan o Bitmap Index Scan).
-- En una tabla pequeña el planificador elige Seq Scan aunque el índice exista, y es correcto.
```

---

## Migración para agregar RLS a una tabla existente

```sql
-- Paso 1: Actualizar la app para fijar app.current_tenant_id, con la tabla todavía sin RLS

-- Paso 2: Crear la política y activar RLS
CREATE POLICY tenant_branch_policy ON dbo.production_orders
    FOR ALL
    USING (
        tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::BIGINT
    );

ALTER TABLE dbo.production_orders ENABLE ROW LEVEL SECURITY;

-- Paso 3: Agregar FORCE para que también alcance al dueño de la tabla
ALTER TABLE dbo.production_orders FORCE ROW LEVEL SECURITY;
```

La app se actualiza primero porque `ENABLE` sin `FORCE` solo deja fuera al dueño de la tabla. Si la app entra con un rol que no es dueño, como el `app_user` de esta nota, RLS la filtra desde el paso 2: sin la variable fijada, una tabla con 2 filas pasa a devolver 0, sin error.

---

## Limitaciones de RLS

| Limitación | Consecuencia |
|-----------|-------------|
| Connection pooling: un valor con alcance de sesión puede sobrevivir entre requests | `set_config(..., true)` dentro de una transacción, o el interceptor que lo reescribe en cada apertura |
| Un superusuario o un rol con `BYPASSRLS` se salta RLS siempre, también con `FORCE` | Usuario de app sin `SUPERUSER` ni `BYPASSRLS` |
| El dueño de la tabla se salta RLS | `FORCE ROW LEVEL SECURITY` |
| Una función `SECURITY DEFINER` corre como su dueño: si es superusuario, o dueño de la tabla sin `FORCE`, ve todas las filas | Evitarlas en tablas con RLS, o darles un dueño sujeto a la política |
| Complejidad operacional | Documentar bien qué tablas tienen RLS y cómo se establece el setting |

---

## Checklist

- [ ] `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` en tablas multi-tenant
- [ ] Política `FOR ALL` con `USING (tenant_id = NULLIF(current_setting(..., true), '')::BIGINT)`
- [ ] Usuario de la app no es superusuario y no tiene `BYPASSRLS`
- [ ] `set_config` con parámetro para fijar el tenant_id, nunca `SET` interpolado
- [ ] Interceptor EF Core sobrescribe `ConnectionOpened` y `ConnectionOpenedAsync`
- [ ] Índices en `(tenant_id)` y `(tenant_id, branch_id)` para rendimiento
- [ ] Verificar con `EXPLAIN ANALYZE`, con volumen real, que el filtro de RLS usa el índice

---

## Glosario

| Término | Definición |
|---------|-----------|
| Row Level Security (RLS) | mecanismo de PostgreSQL que filtra automáticamente las filas a nivel de motor según políticas definidas por tabla |
| Política RLS | regla declarada con `CREATE POLICY` que define qué filas son visibles o modificables para una sesión |
| FORCE ROW LEVEL SECURITY | modificador que aplica RLS también al propietario de la tabla; no alcanza a un superusuario ni a un rol con `BYPASSRLS` |
| SET LOCAL | variante de `SET` que aplica la configuración solo durante la transacción actual; no acepta parámetros |
| set_config | función de PostgreSQL que fija una variable con parámetros; con `true` como tercer argumento equivale a `SET LOCAL` |
| current_setting | función de PostgreSQL que lee una variable de sesión (`app.current_tenant_id`) usada por las políticas RLS |
| app.current_tenant_id | variable de sesión personalizada que establece el contexto del tenant activo en cada conexión |
| BYPASSRLS | privilegio de PostgreSQL que permite a un rol ignorar todas las políticas RLS — nunca conceder al usuario de la app |
| SECURITY DEFINER | función que se ejecuta con los privilegios del dueño de la función, no del llamador — puede bypassar RLS |
| EF Core Interceptor | clase que intercepta eventos del ciclo de vida de EF Core (apertura de conexión, guardado) para ejecutar código transversal |
| Fuga de datos entre tenants | acceso a datos de otro tenant debido a ausencia o mala configuración de aislamiento en multi-tenancy |

---

*Rogelio Arriaga Gonzalez*
