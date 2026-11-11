# Glosario - M01 - Superficie e inventario

> Definiciones de los términos técnicos del módulo 1. Ordenado alfabéticamente.

---

## A

### Allowlist (lista de permitidos)
**Categoría:** Defensa
**Definición:** Lista de lo que se acepta; todo lo demás se rechaza. Es la forma segura de decidir sobre rutas, orígenes y destinos, frente a la lista de lo prohibido.
**En el curso:** Se usa desde el módulo 1 (rutas documentadas) y reaparece en CORS (módulo 6) y en destinos de SSRF (módulo 5).

### AllowAnonymous
**Categoría:** ASP.NET Core
**Definición:** Metadato de una ruta que dice que no exige sesión. La política de autorización no se aplica a esa ruta.
**En el curso:** `POST /api/auth/login` es la única ruta con este metadato en la API de práctica; la prueba `Solo_el_login_es_publico` vigila que no aparezcan otras.

### API (interfaz de programación de aplicaciones)
**Categoría:** Conceptos
**Definición:** Conjunto de reglas para que un programa use las funciones de otro. En una API web, esas reglas son rutas, métodos y formatos sobre HTTP.
**En el curso:** El objeto de todo el curso.

### API Security Top 10
**Categoría:** Estándares
**Definición:** Lista de OWASP de los diez riesgos más críticos de las APIs. La edición vigente a noviembre de 2026 es la de 2023.
**En el curso:** Es el mapa de los seis módulos.

### ASVS
**Categoría:** Estándares
**Definición:** *Application Security Verification Standard* de OWASP: requisitos de seguridad verificables. La versión 5.0.0 se publicó el 30 de mayo de 2025.
**En el curso:** Lista de verificación del informe (módulo 6).

### Autenticación
**Categoría:** Conceptos
**Definición:** Comprobar quién es quien llama. Se estudia en el módulo 2.
**En el curso:** Segundo control de cada solicitud.

### Autorización
**Categoría:** Conceptos
**Definición:** Decidir qué puede hacer quien ya está autenticado, sobre qué objeto y con qué propiedades. Se estudia en el módulo 3.
**En el curso:** Tercer control de cada solicitud y la falla más común en APIs.

---

## B

### BFLA
**Categoría:** Vulnerabilidades
**Definición:** *Broken Function Level Authorization* (API5:2023): una persona usa una función a la que su rol no da derecho.
**En el curso:** Módulo 3.

### BOLA
**Categoría:** Vulnerabilidades
**Definición:** *Broken Object Level Authorization* (API1:2023): una persona accede a un objeto de otra cambiando el identificador, porque el servidor no comprueba de quién es el objeto.
**En el curso:** Módulo 3. En el módulo 1 solo se eligen las rutas candidatas.

---

## C

### Contrato de rutas
**Categoría:** Inventario
**Definición:** Archivo, mantenido por el equipo, con las rutas que **deben** existir. Se compara con lo que la aplicación expone de verdad.
**En el curso:** `rutas-documentadas.txt`.

### crAPI
**Categoría:** Laboratorios
**Definición:** *completely ridiculous API*, proyecto de OWASP: una API "vulnerable por diseño" para practicar los riesgos del Top 10. Licencia Apache-2.0.
**En el curso:** Laboratorio opcional, en una máquina virtual tuya y en red aislada.

---

## E

### EndpointDataSource
**Categoría:** ASP.NET Core
**Definición:** Origen de los *endpoints* (rutas con sus metadatos) que el enrutamiento usa para despachar cada solicitud.
**En el curso:** De aquí se calcula el inventario real.

### Endpoint
**Categoría:** ASP.NET Core
**Definición:** En ASP.NET Core, una ruta resuelta en tiempo de ejecución: el método HTTP, el patrón de ruta y su controlador, más sus metadatos. Se distingue de la *operación* de OpenAPI, que es su descripción.
**En el curso:** La unidad de inventario.

---

## F

### FallbackPolicy
**Categoría:** ASP.NET Core
**Definición:** Política de autorización que se aplica a las rutas que no declaran ninguna. Por omisión es nula (sin exigencia); configurarla para exigir sesión protege las rutas nuevas.
**En el curso:** La línea que hace que "las rutas del futuro nazcan protegidas".

### Fantasma (ruta)
**Categoría:** Inventario
**Definición:** Ruta que el contrato menciona pero que la aplicación ya no expone.
**En el curso:** Señal de que la documentación dejó de ser cierta.

---

## H

### HTTP
**Categoría:** Protocolos
**Definición:** Protocolo de la web: solicitudes con método, ruta, cabeceras y cuerpo, y respuestas con código de estado. RFC 9110 define su semántica.
**En el curso:** Base de todo el curso.

---

## I

### Idempotente
**Categoría:** Protocolos
**Definición:** Un método es idempotente si repetirlo tiene el mismo efecto que hacerlo una vez (RFC 9110, 9.2). `GET`, `PUT` y `DELETE` lo son; `POST`, por definición, no.
**En el curso:** Reaparece en flujos de negocio (módulo 5).

### Inventario
**Categoría:** Inventario
**Definición:** Lista de lo que se expone: hosts, entornos, versiones, rutas, audiencia, autenticación, documentación, terceros y dueño.
**En el curso:** El producto del módulo.

---

## L

### Laboratorio (interruptor de)
**Categoría:** Curso
**Definición:** Opción `Lab:*` de la API de práctica que reintroduce, a propósito, una falla clásica para que su prueba se vea fallar. Todos arrancan apagados.
**En el curso:** Solo existen en la aplicación de práctica.

---

## M

### Marco legal y ético
**Categoría:** Curso
**Definición:** Condiciones para probar la seguridad de un sistema: autorización escrita, alcance, laboratorio propio. En México, el acceso sin autorización es delito (Código Penal Federal, arts. 211 bis 1 a 211 bis 7).
**En el curso:** Va al frente de cada práctica.

---

## O

### OpenAPI
**Categoría:** Estándares
**Definición:** Especificación independiente del lenguaje para documentar APIs HTTP. En ASP.NET Core, `Microsoft.AspNetCore.OpenApi` genera el documento a partir de las rutas.
**En el curso:** El contrato "de verdad" en proyectos grandes.

### OWASP
**Categoría:** Organizaciones
**Definición:** *Open Worldwide Application Security Project*: comunidad que publica estándares y guías de seguridad de software, como el Top 10 y el ASVS.
**En el curso:** Fuente de las listas de riesgos.

---

## P

### Política por omisión
**Categoría:** Defensa
**Definición:** Comportamiento que aplica una regla cuando nadie declara otra. Si lo seguro es lo que ocurre por omisión, un olvido no abre una ruta.
**En el curso:** `FallbackPolicy` que exige sesión.

---

## R

### Reconocimiento (*reconnaissance*)
**Categoría:** Metodología
**Definición:** Fase en la que se reúne información sobre un sistema antes de probarlo: qué expone, cómo se documenta, qué versiones tiene.
**En el curso:** Se estudia desde la defensa: qué revela tu API y cómo reducirlo.

### Ruta olvidada
**Categoría:** Inventario
**Definición:** Ruta que existe en el código pero no en el inventario: depuración, pruebas o una versión vieja.
**En el curso:** La falla del módulo 1 (API9:2023).

---

## S

### Seguridad por oscuridad
**Categoría:** Conceptos
**Definición:** Confiar en que nadie conoce un detalle (una ruta, una clave escondida) en lugar de protegerlo. No es un control.
**En el curso:** El error típico de "esa ruta no la conoce nadie".

### Superficie de ataque
**Categoría:** Conceptos
**Definición:** Todo lo que alguien puede alcanzar y usar de un sistema: rutas, versiones, hosts, entornos, documentación y datos.
**En el curso:** Lo que el inventario enumera.

---

## V

### Versión (de una API)
**Categoría:** Inventario
**Definición:** Variante de la API que se publica a la vez que otras (por ejemplo, `/v1` y `/v2`). Las versiones viejas que siguen expuestas sin parches son un riesgo de API9:2023.
**En el curso:** Cada una lleva su plan de retiro.

---

## W

### WSTG
**Categoría:** Estándares
**Definición:** *Web Security Testing Guide* de OWASP: guía de pruebas de seguridad web. La versión estable a noviembre de 2026 es la 4.2.
**En el curso:** Referencia de método, junto con el Top 10 y el ASVS.
