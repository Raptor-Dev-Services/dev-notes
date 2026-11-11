# 09 - Seguridad en GraphQL: la superficie que se acota

> Lectura del curso [Seguridad de APIs: atacar y defender](../cursos/api-security/README.md), módulo 5.

## Por qué importa

En una API REST cada recurso tiene su ruta, y los límites, la autorización y la documentación se piensan ruta por ruta. GraphQL cambia el reparto: hay **un** punto de entrada y es el cliente quien describe, en cada petición, qué datos quiere y cuántos niveles de relaciones quiere recorrer. El servidor ya no controla la forma de la respuesta; controla lo que **cuesta** y lo que **se permite**.

Esta nota estudia GraphQL **desde la defensa**: qué es cada riesgo, por qué ocurre, cómo se detecta, cómo se corrige y cómo se verifica la corrección. No trae consultas de ataque. Los ejemplos de esquema y los nombres son **ejemplos ilustrativos**. Los fragmentos de configuración de Hot Chocolate salen de su documentación de la versión 15 (consultada en noviembre de 2026) y **no se compilaron** en este curso: van marcados como fragmento.

## Qué es GraphQL, en lo que importa para seguridad

GraphQL es un lenguaje de consulta para APIs y un entorno de ejecución. La especificación (edición de octubre de 2021) define, entre otras cosas, un sistema de tipos, las operaciones de lectura (*query*) y escritura (*mutation*), y en su sección 4 la **introspección**: la capacidad de consultar al servidor por su propio esquema.

| Idea | Consecuencia de seguridad |
|---|---|
| Un solo endpoint | Los límites por ruta no distinguen una consulta barata de una cara |
| El cliente elige los campos | Se pueden pedir campos que el cliente no debería ver |
| El esquema es consultable | Quien pregunta obtiene el mapa de tipos y campos |
| Una petición puede llevar varias operaciones | Un límite por petición HTTP no cuenta las operaciones |
| Los tipos se declaran en el esquema | Ayudan a validar, pero un `String` sigue siendo un texto del cliente |

*Black Hat GraphQL* (Aleks y Farhi, 2023), cap. 3, mapea la superficie de ataque de GraphQL; los caps. 4 a 7 recorren reconocimiento, denegación de servicio, divulgación de información y evasión de autenticación y autorización. Aquí se leen como una lista de **lo que tu servidor debe resistir**.

## Un esquema de ejemplo

Ejemplo ilustrativo (librería ficticia). Observa la relación circular: un autor tiene libros y un libro tiene autor.

```graphql
type Autor {
  id: ID!
  nombre: String!
  libros: [Libro!]!
}

type Libro {
  id: ID!
  titulo: String!
  autor: Autor!
  precioCosto: Float   # dato interno: no deberia verlo cualquiera
}

type Query {
  autor(id: ID!): Autor
  libros(busqueda: String): [Libro!]!
}
```

Tres cosas que un revisor anota al leerlo, sin ejecutar nada:

1. **Ciclo** `Autor -> libros -> autor -> libros...`: una consulta puede anidarse sin fin.
2. **Lista sin tope** (`libros`): no declara paginación.
3. **Campo sensible** (`precioCosto`) sin marca de autorización.

## Los cinco frentes

El *GraphQL Cheat Sheet* de OWASP agrupa sus recomendaciones en entrada, denegación de servicio, control de acceso, ataques por lotes y configuración segura. Aquí se ordenan por frente de defensa.

### 1. Profundidad, cantidad y tiempo

**Qué es y por qué ocurre.** Con relaciones circulares, el costo de una consulta crece con cada nivel de anidación y con cada lista que recorre. Como el cliente describe la consulta, el servidor no sabe de antemano cuánto trabajo le llegará.

**Cómo se detecta.** Leyendo el esquema (¿hay ciclos?, ¿hay listas sin tope?) y midiendo el tiempo de las consultas más profundas **de tu propio esquema** en un entorno de pruebas.

**Cómo se corrige.** OWASP recomienda limitar la profundidad y la cantidad de elementos que puede pedir una consulta, paginar, poner tiempos máximos (en la aplicación, en la infraestructura o en ambas, y dice que los de la aplicación suelen ser más efectivos) y, si hace falta, analizar el costo antes de ejecutar (la guía reconoce que es complejo y puede no ser necesario).

Fragmentos de la documentación de Hot Chocolate 15 (**no compilados**):

```csharp
// FRAGMENTO. Hot Chocolate 15, "Request Limits".
builder.AddGraphQL()
    .AddMaxExecutionDepthRule(10);          // profundidad de ejecucion

builder.AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);   // tiempo maximo
    });

builder.AddGraphQL()
    .ModifyParserOptions(o => { o.MaxAllowedRecursionDepth = 100; })              // anidacion en el analizador
    .ModifyValidationOptions(o => o.MaxAllowedSelectionSetDepth = 100);           // profundidad en la validacion
```

Los números son los de los ejemplos de la documentación: **no son recomendaciones**. Tu límite sale de tu esquema y de lo que mides. La página de seguridad de Hot Chocolate describe además el análisis de costo (límites de costo de campo, de tipo o de tamaño de respuesta) y el tope de elementos por conexión paginada.

**Cómo se verifica.** Con una prueba, escrita **sobre tu propio esquema**, que (a) acepta una consulta normal, (b) rechaza una que supera el límite de profundidad que configuraste y (c) comprueba que una consulta lenta termina con el error de tiempo. Se valida **quitando el control** y viendo caer la prueba.

### 2. Introspección

**Qué es y por qué ocurre.** La introspección está en la especificación y sirve a las herramientas de desarrollo; en producción entrega el mapa completo de tipos y campos a quien lo pida.

**Cómo se detecta.** Preguntando al entorno de producción **propio** si responde a la consulta de esquema, o revisando la configuración.

**Cómo se corrige.** OWASP: desactivarla, y también GraphiQL, en producción o en entornos de acceso público; si se limita en lugar de desactivarla, desactivar también las **sugerencias de campos**, porque permiten adivinar nombres.

```csharp
// FRAGMENTO. Hot Chocolate 15, "Introspection".
builder.AddGraphQL()
    .AllowIntrospection(false);
```

La documentación muestra además cómo permitirla solo para ciertas peticiones con un interceptor, útil para herramientas internas autenticadas.

**Cómo se verifica.** Una prueba en la configuración de producción que comprueba que la consulta de esquema falla. **Límite:** desactivar la introspección no protege los datos; el esquema puede inferirse por otras vías. Lo que protege es la autorización.

### 3. Autorización por objeto y por campo

**Qué es y por qué ocurre.** Un `autor(id: "...")` devuelve lo que la resolución decida devolver. Si la resolución no comprueba que el objeto corresponde al usuario, saber un identificador basta. Si un campo (`precioCosto`) no exige su propio permiso, cualquier usuario autenticado puede pedirlo.

**Cómo se detecta.** Con **dos usuarios** de prueba: cada uno pide el mismo objeto y el mismo campo; el segundo no debe recibir lo que no le toca. Y revisando cada resolución: ¿comprueba el dueño?

**Cómo se corrige.** OWASP: comprobar el acceso a nivel de objeto en cada petición (conocer un identificador no da derecho), controlar el acceso por campo según el permiso, autorizar las mutaciones, y quitar las búsquedas genéricas `node`/`nodes` que no se necesiten. Es la misma idea que BOLA, BOPLA y BFLA del Top 10 de APIs (módulo 3), en la forma de GraphQL: la autorización vive en cada resolución o en una política central, **no** en el esquema como adorno.

**Cómo se verifica.** La prueba de dos usuarios, que debe **caer** cuando se quita la comprobación.

### 4. Alias y lotes

**Qué es y por qué ocurre.** GraphQL permite pedir varias operaciones en una petición (alias, *batching*). Según OWASP, eso permite empacar muchos intentos en una sola petición y así eludir los límites de red por petición.

**Cómo se detecta.** Comparando lo que ve el limitador (peticiones HTTP) con lo que ejecuta el servidor (operaciones u objetos).

**Cómo se corrige.** OWASP recomienda contar objetos en el código, limitar cuántas operaciones caben en una petición y desactivar los lotes en operaciones sensibles como el inicio de sesión o la verificación de códigos. El límite de frecuencia del módulo 4 debe medir **operaciones**, no solo peticiones.

**Cómo se verifica.** Una prueba que envía una petición con más operaciones que el tope y espera el rechazo; se valida quitando el tope.

### 5. Inyección y validación de entrada

Los argumentos de una consulta son entradas. OWASP recomienda validarlos con listas de permitidos y tipos estrictos, y usar consultas parametrizadas o métodos seguros del ORM: la misma defensa que en cualquier API (módulo 4). GraphQL tipa los argumentos, lo cual ayuda, pero no sustituye ni a la validación ni a parametrizar.

## Cómo se ve la revisión de una API GraphQL

Una revisión defensiva de una API propia sigue este orden:

| Paso | Qué haces | Qué produce |
|---|---|---|
| 1 | Lees el esquema | Lista de ciclos, listas sin tope y campos sensibles |
| 2 | Revisas la configuración del servidor | Qué límites, tiempos e introspección tiene |
| 3 | Recorres cada resolución | Dónde se comprueba objeto y campo, y dónde no |
| 4 | Comparas el limitador con las operaciones | Si los lotes eluden el límite |
| 5 | Escribes una prueba por control | Pruebas que caen al quitar el control |
| 6 | Redactas hallazgos | Clase, corrección y prueba, sin consultas de ataque |

## Laboratorios para practicar

Solo en tu máquina virtual, en una red aislada. **No se ejecutaron al escribir esta nota**; siguen los README oficiales.

- **Damn Vulnerable GraphQL Application** (Farhi, McKinnon y Aleks; licencia MIT). Su README indica construir la imagen con `docker build -t dvga .` y correrla con `docker run -d -t -p 5013:5013 -e WEB_HOST=0.0.0.0 --name dvga dvga`, o usar la imagen publicada `dolevf/dvga`; se abre en `http://localhost:5013`. El README menciona una colección de Postman pública con soluciones.
- **OWASP crAPI** sirve para los flujos de negocio del módulo 5; ver su README (`deploy/docker`).

## Resumen

- GraphQL mueve la forma de la consulta al cliente; el servidor tiene que acotar lo que cuesta y lo que permite.
- Se acotan **profundidad, tiempo y costo**, la **introspección**, los **lotes y alias**, y se autoriza **por objeto y por campo**; los argumentos se validan y parametrizan.
- Cada control se configura en el servidor y se demuestra con una prueba que cae al quitarlo.
- Los números de la documentación son ejemplos; el límite lo fija tu esquema.

## Glosario

| Término | Definición |
|---|---|
| Alias | Nombre que permite pedir varias veces el mismo campo en una consulta |
| Costo de una consulta | Estimación del trabajo que implica ejecutarla |
| *Batching* (lotes) | Enviar varias operaciones en una sola petición |
| Introspección | Capacidad de consultar al servidor por su esquema (sección 4 de la especificación) |
| Mutación (*mutation*) | Operación de escritura |
| Profundidad | Niveles de anidación de una consulta |
| Resolución (*resolver*) | Código que produce el valor de un campo |
| Sugerencias de campos | Mensajes de error que proponen nombres parecidos y dejan adivinar el esquema |

## Referencias

- Aleks, N. y Farhi, D. (2023). *Black Hat GraphQL*. No Starch Press. Caps. 3 a 7.
- ChilliCream. (s. f.). *Security*; *Request limits*; *Introspection*. En *Hot Chocolate 15*. https://chillicream.com/docs/hotchocolate/v15/security
- Farhi, D., McKinnon, C. y Aleks, N. (s. f.). *Damn Vulnerable GraphQL Application* [Repositorio]. GitHub. https://github.com/dolevf/Damn-Vulnerable-GraphQL-Application
- GraphQL Foundation. (2021). *GraphQL* (October 2021 Edition). https://spec.graphql.org/October2021/
- OWASP. (s. f.). *GraphQL Cheat Sheet*. OWASP Cheat Sheet Series. https://cheatsheetseries.owasp.org/cheatsheets/GraphQL_Cheat_Sheet.html
- OWASP. (2023). *OWASP API Security Top 10 2023*. https://api-security.owasp.org/editions/2023/en/0x11-t10

---

*Rogelio Arriaga Gonzalez*
