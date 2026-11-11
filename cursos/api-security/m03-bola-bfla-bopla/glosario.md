# Glosario - M03 - Autorización rota: BOLA, BOPLA y BFLA

> Definiciones de los términos técnicos del módulo 3. Ordenado alfabéticamente.

---

## A

### ABAC
**Categoría:** Control de acceso
**Definición:** Control de acceso basado en atributos: la decisión depende de atributos del usuario, del recurso y del contexto, no solo de un rol.
**En el curso:** OWASP lo recomienda sobre RBAC cuando las reglas son finas o multi-inquilino.

### AuthorizeAsync
**Categoría:** ASP.NET Core
**Definición:** Método de `IAuthorizationService` que evalúa una política sobre un usuario y, opcionalmente, un recurso concreto (autorización por recurso).
**En el curso:** Se menciona como fragmento para decidir quién puede qué sobre un pedido.

### Autorización
**Categoría:** Conceptos
**Definición:** Decidir qué puede hacer quien ya se autenticó, sobre qué objeto y con qué propiedades. Es distinta de autenticar.
**En el curso:** El tema del módulo.

---

## B

### BFLA
**Categoría:** Vulnerabilidades
**Definición:** *Broken Function Level Authorization* (API5:2023): se usa una función que no corresponde al rol, por ejemplo una de administración. También incluye cambiar el método HTTP de una ruta que solo protegía otro.
**En el curso:** Se corrige con denegar por omisión y política por rol; se prueba en la matriz.

### BOLA
**Categoría:** Vulnerabilidades
**Definición:** *Broken Object Level Authorization* (API1:2023): la API recibe un id de objeto y no comprueba que quien llama pueda usarlo. En otros contextos se conoce como IDOR.
**En el curso:** Tema de la Parte 1; el primer lugar del Top 10 de 2023.

### BOPLA
**Categoría:** Vulnerabilidades
**Definición:** *Broken Object Property Level Authorization* (API3:2023): se leen o se escriben propiedades de un objeto que quien llama no debería. Reúne Excessive Data Exposure y Mass Assignment de la lista de 2019.
**En el curso:** Se corrige con DTO de entrada y de salida.

---

## D

### Denegar por omisión
**Categoría:** Defensa
**Definición:** Principio: el acceso se concede solo cuando se puede justificar de forma explícita; todo lo demás se niega.
**En el curso:** `FallbackPolicy` y políticas por rol.

### DTO (*data transfer object*)
**Categoría:** Diseño
**Definición:** Objeto que describe lo que entra o sale de la API, separado de la entidad de la base de datos. Funciona como lista de permitidos de propiedades.
**En el curso:** `PerfilCambio` (entrada) y `PerfilDto` (salida).

---

## E

### Excessive Data Exposure
**Categoría:** Vulnerabilidades
**Definición:** Categoría de la lista de 2019: la API devuelve más datos de los necesarios. Hoy forma parte de API3:2023.
**En el curso:** "Leer de más".

---

## F

### Filtro global de consulta
**Categoría:** EF Core
**Definición:** Condición que EF Core agrega a todas las consultas de un tipo (por ejemplo, por dueño o inquilino).
**En el curso:** Hace que las consultas nazcan acotadas.

---

## G

### GUID
**Categoría:** Identificadores
**Definición:** Identificador único de 128 bits. Usado como id de registro dificulta el recorrido al azar, pero no sustituye a la comprobación de dueño.
**En el curso:** OWASP lo recomienda como preferencia, junto con pruebas de autorización.

---

## I

### IDOR
**Categoría:** Vulnerabilidades
**Definición:** *Insecure Direct Object Reference*: referencia directa a un objeto sin comprobar acceso. Es el nombre clásico de BOLA.
**En el curso:** Equivale a API1:2023.

---

## M

### Mass Assignment (asignación masiva)
**Categoría:** Vulnerabilidades
**Definición:** El cliente puede escribir propiedades que no le tocan porque la API enlaza la entrada a un objeto interno.
**En el curso:** "Escribir de más".

### Matriz de autorización
**Categoría:** Pruebas
**Definición:** Tabla que cruza actores (sin sesión, usuarios, roles) con llamadas y la respuesta que debe recibir cada uno. Se escribe como prueba automática.
**En el curso:** Parte 4.

### Multi-tenant (multi-inquilino)
**Categoría:** Arquitectura
**Definición:** Un mismo sistema da servicio a varias organizaciones con datos separados.
**En el curso:** BOLA se vuelve fuga entre inquilinos.

---

## O

### Object level / property level / function level
**Categoría:** Conceptos
**Definición:** Los tres niveles en que puede fallar la autorización: el objeto (este pedido), la propiedad (este campo) y la función (esta operación).
**En el curso:** BOLA, BOPLA y BFLA.

---

## P

### Política de autorización
**Categoría:** ASP.NET Core
**Definición:** Conjunto de requisitos que el framework evalúa para decidir si un usuario puede acceder a un recurso.
**En el curso:** `Admin`, `FallbackPolicy`.

### Privilegio mínimo
**Categoría:** Defensa
**Definición:** Cada usuario, rol y proceso recibe solo el acceso que necesita.
**En el curso:** Principio de diseño detrás de los roles y de las matrices.

---

## R

### RBAC
**Categoría:** Control de acceso
**Definición:** Control de acceso basado en roles: los permisos se asignan a roles y los roles a usuarios.
**En el curso:** `RequireRole("admin")`.

### ReBAC
**Categoría:** Control de acceso
**Definición:** Control de acceso basado en relaciones entre usuario y recurso (por ejemplo, "es dueño de").
**En el curso:** La comprobación de dueño es la forma más simple.

---

## V

### "Verlo fallar"
**Categoría:** Metodología del curso
**Definición:** Reintroducir a propósito la falla (con un interruptor del laboratorio o quitando una línea) y leer el error de la prueba, para saber que la prueba protege.
**En el curso:** Paso de cada práctica.
