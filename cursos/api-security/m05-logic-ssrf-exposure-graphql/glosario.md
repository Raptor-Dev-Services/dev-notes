# Glosario - M05 - Lógica de negocio, destinos y exposición de datos; GraphQL

> Definiciones de los términos técnicos del módulo 5. Ordenado alfabéticamente.

---

## A

### Alias (GraphQL)
**Categoría:** GraphQL
**Definición:** Nombre que permite pedir varias veces el mismo campo en una consulta. Junto con los lotes, puede juntar muchas operaciones en una petición.
**En el curso:** Parte 5; el límite de frecuencia debe contar operaciones.

### API10:2023 Unsafe Consumption of APIs
**Categoría:** OWASP API Security
**Definición:** Categoría: se confía demasiado en lo que devuelve una API de terceros y se debilita la validación.
**En el curso:** Parte 4.

### API3:2023 Broken Object Property Level Authorization
**Categoría:** OWASP API Security
**Definición:** Categoría que fusiona la exposición excesiva de datos y la asignación masiva: la API devuelve o acepta propiedades que el cliente no debía ver ni tocar.
**En el curso:** Parte 2.

### API6:2023 Unrestricted Access to Sensitive Business Flows
**Categoría:** OWASP API Security
**Definición:** Categoría: un flujo de negocio (comprar, reservar, aplicar un cupón) se expone sin restricciones y se abusa, normalmente con automatización.
**En el curso:** Parte 1.

### API7:2023 Server Side Request Forgery
**Categoría:** OWASP API Security
**Definición:** Categoría: la API trae un recurso remoto sin validar una URL dada por el usuario.
**En el curso:** Parte 3.

### Atomicidad
**Categoría:** Concurrencia
**Definición:** Propiedad de una operación que ocurre completa o no ocurre, sin que otra la vea a medias. Revisar y marcar un cupón debe ser atómico.
**En el curso:** Parte 1; `lock` y restricción única.

---

## C

### Condición de carrera (race condition)
**Categoría:** Concurrencia
**Definición:** Error que depende del orden en que se intercalan operaciones simultáneas, como revisar y marcar un cupón en pasos separados.
**En el curso:** Parte 1.

### ConnectCallback
**Categoría:** ASP.NET Core / .NET
**Definición:** Función de `SocketsHttpHandler` que decide cómo se abre la conexión. Permite verificar la dirección resuelta antes de conectar.
**En el curso:** Parte 3.

### Contrato de respuesta
**Categoría:** Defensa
**Definición:** Lista de propiedades que una respuesta puede llevar, vigilada por una prueba que afirma lo que no debe estar.
**En el curso:** Parte 2.

### Costo de una consulta
**Categoría:** GraphQL
**Definición:** Estimación del trabajo que implica ejecutar una consulta, usada para rechazar las que pasan un límite.
**En el curso:** Parte 5.

### Cupón
**Categoría:** Negocio
**Definición:** Código que descuenta un porcentaje. En la práctica, `PROMO10` descuenta 10 % una vez por pedido.
**En el curso:** Parte 1.

---

## D

### DNS rebinding
**Categoría:** Red
**Definición:** Técnica en la que un nombre cambia de dirección entre la validación y la conexión. Se defiende conectando solo a la dirección ya validada.
**En el curso:** Parte 3; el cheat sheet de OWASP advierte que revisar el DNS solo no basta.

### DTO (objeto de transferencia)
**Categoría:** Diseño
**Definición:** Clase que declara exactamente lo que una respuesta o una petición lleva, separada de la entidad interna.
**En el curso:** Parte 2; `PerfilDto`.

### DVGA
**Categoría:** Laboratorio
**Definición:** Damn Vulnerable GraphQL Application: aplicación GraphQL vulnerable a propósito, de Dolev Farhi, Connor McKinnon y Nick Aleks (MIT).
**En el curso:** Práctica, paso 9, opcional.

---

## E

### Entidad
**Categoría:** Diseño
**Definición:** Clase interna que representa un registro, con todos sus campos. No se devuelve tal cual al cliente.
**En el curso:** Parte 2.

### EsDireccionInterna
**Categoría:** Práctica
**Definición:** Función de `Destinos.cs` que reconoce direcciones de loopback, redes privadas y enlace local, incluidas las IPv4 mapeadas en IPv6.
**En el curso:** Parte 3.

### Exposición excesiva de datos
**Categoría:** Clase de falla
**Definición:** La respuesta devuelve más propiedades de las que el cliente necesita.
**En el curso:** Parte 2.

---

## F

### Flujo de negocio sensible
**Categoría:** Negocio
**Definición:** Acción de la API cuyo abuso daña al negocio aunque cada petición sea técnicamente válida.
**En el curso:** Parte 1.

---

## I

### Idempotencia
**Categoría:** Diseño
**Definición:** Propiedad de una operación que, repetida con la misma llave, produce el mismo efecto que una sola vez.
**En el curso:** Parte 1; nota `45-idempotency-keys.md`.

### Introspección
**Categoría:** GraphQL
**Definición:** Capacidad de preguntarle al servidor por su esquema. Se desactiva donde no deba haber esquema público.
**En el curso:** Parte 5; `AllowIntrospection(false)` en Hot Chocolate.

---

## L

### Lista de permitidos de destinos
**Categoría:** Defensa
**Definición:** Conjunto de hosts que la API puede visitar, comparado por igualdad exacta.
**En el curso:** Parte 3; `Destinos.EsPermitido`.

### Lotes (batching)
**Categoría:** GraphQL
**Definición:** Enviar varias operaciones en una sola petición. Puede eludir los límites por petición.
**En el curso:** Parte 5.

---

## M

### Mapa de flujos
**Categoría:** Método
**Definición:** Tabla de las acciones con dinero o confianza, la regla que las limita y dónde vive esa regla.
**En el curso:** Parte 1.

### Metadatos de la nube
**Categoría:** Nube
**Definición:** Servicio de metadatos de instancias de nube, en una dirección como `169.254.169.254` (la que cita el cheat sheet de OWASP). Debe ser inalcanzable desde un destino elegido por el cliente.
**En el curso:** Parte 3.

---

## P

### Profundidad de consulta
**Categoría:** GraphQL
**Definición:** Cuántos niveles anida una consulta. Cada nivel puede multiplicar el trabajo del servidor.
**En el curso:** Parte 5; `AddMaxExecutionDepthRule`.

### Prueba de contrato
**Categoría:** Pruebas
**Definición:** Prueba que afirma lo que una respuesta no debe contener.
**En el curso:** Parte 2.

---

## R

### Redirección
**Categoría:** HTTP
**Definición:** Respuesta que manda al cliente a otra URL. Un cliente que las sigue sin control puede llegar a un destino no validado.
**En el curso:** Parte 3; `AllowAutoRedirect = false`.

### Resolución (GraphQL resolver)
**Categoría:** GraphQL
**Definición:** Código que produce el valor de un campo. Es donde se comprueba la autorización por objeto y por campo.
**En el curso:** Parte 5.

---

## S

### SocketsHttpHandler
**Categoría:** ASP.NET Core / .NET
**Definición:** Manejador de bajo nivel de `HttpClient`; expone `AllowAutoRedirect`, `ConnectTimeout` y `ConnectCallback`.
**En el curso:** Parte 3.

### SSRF
**Categoría:** Clase de falla
**Definición:** Falsificación de peticiones del lado del servidor: la API trae un recurso remoto que eligió el cliente.
**En el curso:** Parte 3.

### Sugerencias de campos
**Categoría:** GraphQL
**Definición:** Mensajes de error que proponen nombres de campos parecidos; dejan adivinar el esquema aunque la introspección esté apagada.
**En el curso:** Parte 5; OWASP recomienda desactivarlas.

---

## T

### Tiempo máximo de ejecución
**Categoría:** Límites
**Definición:** Tope de duración de una consulta; en Hot Chocolate, `ExecutionTimeout`.
**En el curso:** Parte 5.

### TLS
**Categoría:** Transporte
**Definición:** Protocolo que cifra el transporte. OWASP pide que toda interacción con APIs externas vaya por canales cifrados.
**En el curso:** Parte 4.

---

## V

### Vista previa de URL
**Categoría:** Función
**Definición:** Función que visita una URL para mostrar una miniatura o un resumen; terreno típico de SSRF.
**En el curso:** Parte 3; `/api/vistas-previas`.

---

## W

### Webhook
**Categoría:** Integración
**Definición:** Llamada HTTP que un sistema hace a una URL registrada por el cliente. Es otro caso donde la API visita destinos elegidos por otro.
**En el curso:** Parte 3.

---

*Rogelio Arriaga Gonzalez*
