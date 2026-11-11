# Glosario - M5 - I/O, red y base de datos

> Definiciones de los términos técnicos del módulo 5.
> Ordenado alfabéticamente.

---

## A

### AsNoTracking
**Categoría:** EF Core
**Definición:** Operador de consulta de EF Core que indica que las entidades devueltas no se registran en el seguimiento de cambios del contexto. Es generalmente más rápido en lecturas porque no se prepara la información de seguimiento, pero esas entidades no se guardan con `SaveChanges`.
**En el curso:** con índice, EF Core con `AsNoTracking` asignó 91.99 KB y con seguimiento 148.62 KB en la misma consulta.

---

## C

### Caché de página
**Categoría:** Sistema operativo
**Definición:** Memoria que el sistema operativo usa para guardar contenido de archivos ya leídos, de modo que una lectura repetida no vaya al disco.
**En el curso:** el benchmark de lectura de archivos corrió con el archivo ya en la caché, así que no mide el disco.

---

## D

### Dapper
**Categoría:** Acceso a datos
**Definición:** Micro-ORM para .NET que ejecuta SQL escrito a mano y materializa filas en objetos. Tiene menos capas que un ORM completo.
**En el curso:** asignó 20.63 KB por la consulta de 100 filas.

---

## E

### EF Core
**Categoría:** Acceso a datos
**Definición:** Entity Framework Core, el ORM de Microsoft para .NET. Traduce consultas LINQ a SQL y puede dar seguimiento de cambios a las entidades.
**En el curso:** se comparó con Dapper; el módulo 2 de Backend con .NET lo explica.

---

## H

### HttpClient
**Categoría:** Red
**Definición:** Clase de .NET para enviar peticiones HTTP. Cada instancia usa su propio grupo de conexiones; por eso se reutiliza o se obtiene de una fábrica.
**En el curso:** un cliente por petición dejó unos 2 947 sockets más en `TIME_WAIT` en 3 000 peticiones.

---

## I

### IHttpClientFactory
**Categoría:** Red
**Definición:** Fábrica que crea clientes de vida corta y reutiliza los `HttpMessageHandler` en un grupo, para evitar el agotamiento de sockets.
**En el curso:** es una de las dos opciones que recomienda la documentación; la otra es un cliente singleton con `PooledConnectionLifetime`.

### Identity resolution
**Categoría:** EF Core
**Definición:** Comportamiento de las consultas con seguimiento: si el resultado trae la misma entidad varias veces, EF Core devuelve la misma instancia. Las consultas sin seguimiento devuelven instancias nuevas.
**En el curso:** parte de lo que `AsNoTracking` evita pagar.

### Índice
**Categoría:** Base de datos
**Definición:** Estructura que permite a la base encontrar filas por una columna sin recorrer toda la tabla.
**En el curso:** con índice en `cliente_id`, Dapper tardó 264.8 us contra 8 498.0 us sin él, en esa corrida.

---

## L

### Latencia de red
**Categoría:** Red
**Definición:** Tiempo que tarda una ida y vuelta entre dos máquinas. En una conversación con muchas consultas pequeñas se paga una vez por cada una.
**En el curso:** el benchmark de N+1 usó SQLite sin red, así que este costo **no se midió**.

---

## N

### N+1
**Categoría:** Acceso a datos
**Definición:** Patrón en el que se ejecuta una consulta para obtener N elementos y luego una consulta más por cada uno. Se corrige con una consulta que traiga todo (`IN`, `JOIN`, `Include`).
**En el curso:** 100 consultas tardaron 8.861 ms y una sola 6.016 ms, en SQLite.

---

## P

### PooledConnectionLifetime
**Categoría:** Red
**Definición:** Propiedad de `SocketsHttpHandler` que limita cuánto tiempo se reutiliza una conexión. Al reemplazarla se repite la resolución de DNS.
**En el curso:** `HttpClient` solo resuelve DNS al crear la conexión; esta propiedad evita que un cliente largo se quede con una dirección vieja.

---

## R

### ReadLines
**Categoría:** Archivos
**Definición:** `File.ReadLines` devuelve las líneas de un archivo como una secuencia que se lee mientras se recorre, sin cargarlo entero.
**En el curso:** no tuvo recolecciones de gen 2 en la medición, a diferencia de `ReadAllLines`.

---

## S

### SocketsHttpHandler
**Categoría:** Red
**Definición:** La implementación de `HttpClient` desde .NET Core 2.1, con su propio grupo de conexiones.
**En el curso:** se configura con `PooledConnectionLifetime`.

### SQLite
**Categoría:** Base de datos
**Definición:** Motor de base de datos embebido: corre dentro del proceso y guarda todo en un archivo.
**En el curso:** se usó para los benchmarks de acceso a datos; no sirve para predecir el tiempo de una base por red.

---

## T

### TIME_WAIT
**Categoría:** Red
**Definición:** Estado de TCP en el que un socket cerrado sigue ocupando su puerto un tiempo antes de liberarse (RFC 9293, sección 3.3.2). Con muchas conexiones cortas pueden acumularse y agotar los puertos.
**En el curso:** se contó leyendo `/proc/net/tcp` en Linux (estado `06`).

### Tracking (seguimiento)
**Categoría:** EF Core
**Definición:** Registro, en el contexto, de las entidades leídas para detectar cambios y guardarlos con `SaveChanges`. Es el comportamiento por omisión de EF Core.
**En el curso:** es lo que `AsNoTracking` desactiva.
