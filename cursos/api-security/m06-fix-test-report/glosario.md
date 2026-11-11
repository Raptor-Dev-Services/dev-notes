# Glosario - M06 - Corregir, probar y reportar

> Definiciones de los términos técnicos del módulo 6. Ordenado alfabéticamente.

---

## A

### API1:2023 a API10:2023
**Categoría:** Top 10
**Definición:** Las diez categorías de la edición 2023 del OWASP API Security Top 10, de Broken Object Level Authorization a Unsafe Consumption of APIs.
**En el curso:** La lista que ordena controles y pruebas de todo el módulo.

### ASVS
**Categoría:** Estándar
**Definición:** Application Security Verification Standard de OWASP: lista de requisitos de seguridad para aplicaciones. Su última versión estable a noviembre de 2026 es la 5.0.0, del 30 de mayo de 2025.
**En el curso:** Lista de verificación al cerrar el proyecto final.

### Arreglado frente a cerrado
**Categoría:** Proceso
**Definición:** Un hallazgo está arreglado cuando se corrigió la causa; está cerrado cuando además tiene una prueba automatizada que corre en cada cambio.
**En el curso:** Es la diferencia que sostiene el lema del módulo.

---

## B

### Bloqueo del merge
**Categoría:** CI
**Definición:** Regla de protección de la rama que exige que una verificación (por ejemplo, las pruebas de seguridad) pase antes de integrar un cambio. Lo decide la configuración de la rama, no el archivo del flujo.
**En el curso:** Paso 7 de la práctica.

---

## C

### Cache-Control: no-store
**Categoría:** Cabecera
**Definición:** Indica que la respuesta no debe guardarse en cachés. Se usa en respuestas con datos de una persona.
**En el curso:** Una de las tres cabeceras del código de referencia.

### Camino feliz
**Categoría:** Pruebas
**Definición:** Prueba con datos válidos y un usuario con permiso. Necesaria pero no suficiente para seguridad.
**En el curso:** Error típico de la parte 1.

### CORS
**Categoría:** Navegador
**Definición:** Mecanismo con el que el navegador decide si una página de un origen puede leer respuestas de otro. Lo aplica el navegador: no sustituye a la autenticación.
**En el curso:** Se configura con una lista de orígenes.

### Content-Security-Policy
**Categoría:** Cabecera
**Definición:** Política que el navegador aplica al contenido. Con `frame-ancestors 'none'` la respuesta no se muestra dentro de marcos.
**En el curso:** Cabecera del código de referencia.

### Corrección de causa
**Categoría:** Proceso
**Definición:** Cambio que elimina la razón del fallo (por ejemplo, filtrar por dueño en toda consulta) en lugar de tapar el caso visto.
**En el curso:** Primer requisito de un hallazgo cerrado.

---

## D

### Divulgación responsable
**Categoría:** Proceso
**Definición:** Práctica de avisar al dueño de un sistema de un posible fallo por su canal de reporte, con lo mínimo para reproducirlo y dándole tiempo de corregir antes de hablar en público.
**En el curso:** Parte 5.

### DTO
**Categoría:** Diseño
**Definición:** Objeto que define exactamente qué entra o sale de la API, para no exponer ni aceptar propiedades de la entidad.
**En el curso:** Control de API3.

---

## E

### Evidencia mínima
**Categoría:** Informe
**Definición:** La petición y la respuesta estrictamente necesarias para mostrar el problema, sin datos reales de personas.
**En el curso:** Campo de la ficha del hallazgo.

---

## F

### Fallback policy
**Categoría:** ASP.NET Core
**Definición:** Política de autorización que se aplica a los puntos de conexión que no tienen ninguna. Con `RequireAuthenticatedUser`, una ruta nueva exige sesión desde que nace.
**En el curso:** Su mutación hace caer varias pruebas.

### Ficha de hallazgo
**Categoría:** Informe
**Definición:** Registro corto de un hallazgo: título, clase, dónde, evidencia mínima, impacto, corrección, prueba y prioridad.
**En el curso:** Paso 6 y proyecto final.

---

## H

### HSTS
**Categoría:** Cabecera
**Definición:** Cabecera `Strict-Transport-Security`. En ASP.NET Core `UseHsts()` la añade con vigencia por omisión de 30 días y se usa normalmente fuera de desarrollo. Para APIs, la documentación recomienda escuchar solo por HTTPS o rechazar HTTP en lugar de redirigir.
**En el curso:** Parte 2.

---

## I

### Inventario de rutas
**Categoría:** API9
**Definición:** Lista de las rutas que la API debe exponer, comparada con las que expone de verdad.
**En el curso:** `Modulo1Tests`.

---

## M

### Modelado de amenazas continuo
**Categoría:** Proceso
**Definición:** Práctica de repetir el modelado cada vez que cambia algo relevante, con cuatro preguntas cortas, en lugar de producir un documento único.
**En el curso:** Parte 4; *Threat Modeling* (2020), cap. 5.

### Mutación
**Categoría:** Pruebas
**Definición:** Cambio deliberado que quita o debilita un control para comprobar que alguna prueba cae.
**En el curso:** Parte 3 y pasos 3 a 5.

---

## N

### nosniff
**Categoría:** Cabecera
**Definición:** Valor de `X-Content-Type-Options` que impide que el navegador adivine el tipo del contenido.
**En el curso:** Cabecera del código de referencia.

---

## P

### Prioridad
**Categoría:** Informe
**Definición:** Nivel (alta, media o baja) con su razón. No se inventa un puntaje: si se usa un sistema formal, se cita su versión.
**En el curso:** Parte 5.

### Prueba hermana
**Categoría:** Pruebas
**Definición:** Prueba que enciende un interruptor de laboratorio para demostrar que el detector sí detecta la falla.
**En el curso:** Nombres con `Verlo_fallar`.

---

## R

### Rojo, verde
**Categoría:** Pruebas
**Definición:** Secuencia que demuestra que una prueba sirve: falla antes del arreglo y pasa después.
**En el curso:** Evidencia exigida en la tarea.

### Regla de protección de rama
**Categoría:** CI
**Definición:** Ajuste del repositorio que condiciona la integración a verificaciones obligatorias.
**En el curso:** Paso 7.

---

## S

### Secreto fuera del código
**Categoría:** Configuración
**Definición:** Credencial o clave que se lee de configuración (variable de entorno o gestor de secretos) y no del repositorio.
**En el curso:** `Jwt:Clave`.

### SetDefaultTimesOnTokenCreation
**Categoría:** JWT
**Definición:** Propiedad del manejador de tokens que, por omisión, añade una expiración al crear un token. Hay que apagarla para construir, en una prueba, un token sin `exp`.
**En el curso:** Hallazgo al escribir la suite.

---

## T

### Trait
**Categoría:** xUnit
**Definición:** Etiqueta de una prueba o clase. `[Trait("Categoria", "Seguridad")]` permite correr solo esas pruebas con `--filter`.
**En el curso:** Parte 4.

---

## V

### ValidateOnStart
**Categoría:** ASP.NET Core
**Definición:** Opción del patrón de opciones que valida la configuración al arrancar y detiene la aplicación si es inválida.
**En el curso:** Rechaza la clave de firma corta.

### Verlo fallar
**Categoría:** Pruebas
**Definición:** Romper algo a propósito, en tu laboratorio, y leer el error para confirmar qué vigila una prueba.
**En el curso:** Sección fija de cada práctica.

---
