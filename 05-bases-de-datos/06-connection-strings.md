# 3 · Connection strings y manejo por ambiente

La cadena de conexión es uno de los secretos más sensibles del sistema. Un leak permite acceso completo a la base de datos. Su manejo merece la misma disciplina que cualquier secreto.

## 3.1 Componentes de una connection string

| **Componente** | **Descripción** |
|----|----|
| **Host / Server** | Hostname o IP del servidor de base de datos |
| **Port** | Puerto. 1433 SQL Server, 5432 Postgres, 3306 MySQL |
| **Database** | Nombre de la base de datos |
| **Username / User Id** | Usuario de la conexión |
| **Password** | Contraseña del usuario |
| **Pooling / Max Pool Size** | Número máximo de conexiones simultáneas. Default 100 |
| **Timeout / Connect Timeout** | Segundos antes de fallar al conectar. Default 15 |
| **TrustServerCertificate / SslMode** | Manejo de SSL/TLS |
| **Application Name** | Identifica la app en logs del servidor de BD. Ponlo siempre. |

## 3.2 PostgreSQL (Npgsql)

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>Postgres connection string</em></td>
</tr>
<tr>
<td><p>Host=db.taskflow.internal;</p>
<p>Port=5432;</p>
<p>Database=taskflow_prod;</p>
<p>Username=app_user;</p>
<p>Password=***;</p>
<p>Maximum Pool Size=100;</p>
<p>Connection Idle Lifetime=300;</p>
<p>Application Name=TaskFlow.Api;</p>
<p>Ssl Mode=Require;</p>
<p>Trust Server Certificate=true</p></td>
</tr>
</tbody>
</table>

## 3.3 SQL Server

<table>
<colgroup>
<col style="width: 100%" />
</colgroup>
<tbody>
<tr>
<td><em>SQL Server connection string</em></td>
</tr>
<tr>
<td><p>Server=db.taskflow.internal,1433;</p>
<p>Database=taskflow_prod;</p>
<p>User Id=app_user;</p>
<p>Password=***;</p>
<p>Max Pool Size=100;</p>
<p>Connection Timeout=15;</p>
<p>Application Name=TaskFlow.Api;</p>
<p>TrustServerCertificate=False;</p>
<p>Encrypt=True</p></td>
</tr>
</tbody>
</table>

## 3.4 Connection pooling

| **Variable** | **Recomendación** |
|----|----|
| **Maximum Pool Size** | 100 por instancia API. Si tienes 4 instancias = 400 conexiones totales a la BD. |
| **BD acepta** | Postgres default acepta 100. SQL Server acepta más. Ajusta el pool al límite del servidor. |
| **Síntoma de pool agotado** | Errores 'timeout obteniendo conexión'. Sube Maximum Pool Size o investiga conexiones no liberadas. |
| **Conexiones zombi** | Si tu app no usa using/await using para cerrar, las conexiones se quedan tomadas. Bug clásico. |



---

*Rogelio Arriaga Gonzalez*
