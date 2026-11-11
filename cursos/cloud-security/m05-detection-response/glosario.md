# Glosario - M05 - Detección y respuesta en la nube

> Definiciones de los términos técnicos del módulo 5.
> Ordenado alfabéticamente.

---

## A

### Addendum (CloudTrail)
**Categoría:** CloudTrail
**Definición:** Campo que aparece en un registro cuando su entrega se retrasó o cuando después se supo información que faltaba. Indica la razón (por ejemplo, retraso de entrega o datos actualizados).
**En el curso:** Explica por qué un evento puede llegar horas después de ocurrido; ordena por `eventTime`, no por llegada.

---

### AccessDenied
**Categoría:** Autorización
**Definición:** Código de error que la API devuelve cuando la identidad no tiene permiso para la acción. CloudTrail lo guarda en `errorCode` junto con el mensaje.
**En el curso:** Tres `AccessDenied` seguidos de una misma identidad sobre archivos distintos fueron la segunda regla de `buscar.py`.

---

### Alarma
**Categoría:** Detección
**Definición:** Regla que vigila una métrica o un patrón y avisa a una persona o a un sistema cuando se cumple una condición.
**En el curso:** Para un `StopLogging`, el umbral es 1: es un evento, no una tendencia.

---

## C

### CloudTrail
**Categoría:** AWS / Registro
**Definición:** Servicio que registra las llamadas a la API de AWS hechas desde la consola, la CLI, los SDK y otros servicios. Ofrece un historial de 90 días de eventos de administración y, con un trail, entrega registros a un bucket.
**En el curso:** Es la fuente principal del módulo.

---

### Contención
**Categoría:** Respuesta a incidentes
**Definición:** Acción que limita el alcance de un incidente: quitar permisos, aislar un recurso, cortar una ruta de red. No es lo mismo que apagar o borrar.
**En el curso:** Se contiene con la misma API que usó el problema, y antes se preserva evidencia.

---

## D

### Datos (eventos de)
**Categoría:** CloudTrail
**Definición:** Eventos del plano de datos, como leer un objeto de S3. No se registran por defecto y tienen cargo adicional.
**En el curso:** Sin ellos, el historial no puede decir qué archivos se leyeron.

---

### Diagnostic setting
**Categoría:** Azure / Registro
**Definición:** Configuración de Azure Monitor que envía registros de recursos, métricas y el Activity Log a destinos como Log Analytics, Event Hubs o una cuenta de almacenamiento.
**En el curso:** Es el equivalente práctico de crear un trail: decide dónde y cuánto se conserva.

---

## E

### eventTime
**Categoría:** CloudTrail
**Definición:** Fecha y hora en UTC en que se completó la solicitud, según el reloj del servicio que la atendió.
**En el curso:** Toda línea de tiempo se arma en UTC.

---

## F

### Falso positivo
**Categoría:** Detección
**Definición:** Alerta que se dispara sin que exista la conducta que se quería detectar.
**En el curso:** Demasiados falsos positivos hacen que la gente silencie la regla.

---

## G

### GuardDuty
**Categoría:** AWS / Detección
**Definición:** Servicio de detección de amenazas que analiza registros de la cuenta con inteligencia de amenazas y aprendizaje automático. Avisa; no previene.
**En el curso:** Complementa tus reglas propias; no sabe qué es normal para tu negocio.

---

## H

### Hallazgo
**Categoría:** Detección
**Definición:** Resultado de una regla o servicio que marca algo para revisión. Aún no es un incidente confirmado.
**En el curso:** La primera tarea de quien responde es clasificar el hallazgo.

---

## I

### Incidente
**Categoría:** Respuesta a incidentes
**Definición:** Evento que afecta o puede afectar la confidencialidad, integridad o disponibilidad y que exige respuesta coordinada. (Definición de trabajo del curso.)
**En el curso:** Se distingue de hallazgo y de evento.

---

### Insights (eventos de)
**Categoría:** CloudTrail
**Definición:** Eventos que señalan un ritmo inusual de llamadas a la API o de errores. Son opcionales y con cargo.
**En el curso:** Alternativa a una regla propia de volumen; no reemplaza las reglas de conducta.

---

## L

### Línea de tiempo
**Categoría:** Respuesta a incidentes
**Definición:** Lista ordenada por hora de lo que ocurrió, con quién, qué y desde dónde, construida con los registros.
**En el curso:** Es el entregable de la práctica.

---

## M

### Management event (evento de administración)
**Categoría:** CloudTrail
**Definición:** Evento del plano de control, como crear un trail o adjuntar una política. Un trail los registra por defecto.
**En el curso:** El historial de 90 días solo muestra de este tipo.

---

### MFAUsed
**Categoría:** Autenticación
**Definición:** Campo de `additionalEventData` en eventos de inicio de sesión de consola que indica si se usó autenticación multifactor.
**En el curso:** Con valor `No` para root fue la primera regla de la práctica.

---

## P

### Plano de control / plano de datos
**Categoría:** Arquitectura
**Definición:** El plano de control es el conjunto de operaciones que configuran recursos; el plano de datos, las que leen o escriben su contenido.
**En el curso:** Cada plano se registra con un mecanismo distinto y se enciende por separado.

---

## R

### Registro de recursos (Azure)
**Categoría:** Azure / Registro
**Definición:** Registros del plano de datos de un recurso de Azure. No se recolectan por defecto: requieren un diagnostic setting.
**En el curso:** Equivalente de los eventos de datos de CloudTrail.

---

## S

### SCP (política de control de servicio)
**Categoría:** AWS / Organizations
**Definición:** Política de AWS Organizations que limita los permisos máximos de las cuentas miembro. No concede permisos y no afecta a la cuenta de administración.
**En el curso:** Sirve para que nadie pueda apagar el trail desde una cuenta miembro.

---

## T

### Tabletop (simulacro de mesa)
**Categoría:** Respuesta a incidentes
**Definición:** Ejercicio de discusión guiada sobre un escenario inventado, sin tocar sistemas, para probar planes y roles.
**En el curso:** Se usó un escenario de Taller Demo.

---

### Trail
**Categoría:** CloudTrail
**Definición:** Configuración que entrega eventos de CloudTrail a un bucket de S3 (y opcionalmente a otros destinos) de forma continua, más allá de los 90 días del historial.
**En el curso:** Debe ser multirregión, con validación de integridad y bucket protegido.

---

## V

### Validación de integridad de archivos
**Categoría:** CloudTrail
**Definición:** Función que genera hashes SHA-256 de cada archivo de registro y un resumen horario firmado con SHA-256 y RSA, para detectar modificaciones o borrados.
**En el curso:** Activarla no valida nada: hay que ejecutar la validación.
