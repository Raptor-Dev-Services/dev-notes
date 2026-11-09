# Glosario - Módulo 6
## Diseño de sistemas y la oferta

> Términos usados en el módulo 6. Ordenado alfabéticamente.

---

## A

### API sin estado
**Categoría:** Diseño de sistemas
**Definición:** API que no guarda datos de sesión en la memoria de su proceso, para que cualquier instancia pueda atender cualquier petición. Es la condición para escalar horizontalmente detrás de un balanceador.

---

## B

### Bruto y neto
**Categoría:** Oferta
**Definición:** El sueldo bruto es el que se pacta antes de impuestos y cuotas; el neto es lo que llega a tu cuenta después de retener ISR y la cuota del trabajador al IMSS. Las ofertas se comparan en neto más prestaciones.

---

## C

### Cache-aside
**Categoría:** Diseño de sistemas
**Definición:** Patrón de caché en el que la aplicación busca primero en la caché; si no encuentra, lee de la base, guarda el resultado con un tiempo de vida y lo devuelve.

---

### Clave caliente
**Categoría:** Diseño de sistemas
**Definición:** Registro que concentra una parte desproporcionada del tráfico, como un enlace viral. Se absorbe con caché, y si hace falta con una copia local en cada instancia.

---

## E

### Entrega al menos una vez
**Categoría:** Diseño de sistemas
**Definición:** Garantía de la mayoría de las colas: ningún mensaje se pierde, pero alguno puede llegar repetido. Obliga a que el consumidor sea idempotente.

---

### Esquema de contratación
**Categoría:** Oferta
**Definición:** Forma legal de la relación de trabajo: nómina, honorarios o contrato como independiente. Define quién paga impuestos y seguridad social y si hay prestaciones de ley.

---

### Estimación de servilleta
**Categoría:** Diseño de sistemas
**Definición:** Cálculo rápido de órdenes de magnitud (peticiones por segundo, almacenamiento) con supuestos dichos en voz alta. Sirve para saber si el sistema cabe en un servidor o en cien.

---

## I

### Idempotencia
**Categoría:** Diseño de sistemas
**Definición:** Propiedad de una operación que, aplicada varias veces, deja el mismo resultado que aplicada una. Un worker que recuerda los ids de los mensajes ya procesados es idempotente.

---

## M

### Matriz ponderada
**Categoría:** Oferta
**Definición:** Tabla que califica cada oferta por criterios con peso (neto efectivo, aprendizaje, estabilidad, modalidad, crecimiento) y suma calificación por peso. Obliga a decidir qué importa antes de ver los números.

---

## P

### Prestaciones de ley
**Categoría:** Oferta
**Definición:** Beneficios mínimos que la Ley Federal del Trabajo obliga a dar en una relación laboral, como aguinaldo, vacaciones, prima vacacional, seguridad social y reparto de utilidades. Sus mínimos se revisan en la versión vigente de la ley.

---

### Punto único de falla
**Categoría:** Diseño de sistemas
**Definición:** Pieza cuya caída tumba el sistema completo, como una sola base sin réplica o un solo worker. Buscarlo es parte del paso de profundizar.

---

## R

### Rango salarial fundamentado
**Categoría:** Oferta
**Definición:** Rango de sueldo con puesto, nivel, ciudad o modalidad, fuente y fecha de cada dato, y aclaración de si es bruto o neto. Es la base de una contrapropuesta seria.

---

### Redirección 301 y 302
**Categoría:** HTTP
**Definición:** 301 es permanente: el navegador la guarda y las visitas repetidas ya no llegan al servidor. 302 es temporal: cada visita pasa por el servidor. Un acortador que cuenta clics usa 302.

---

### Restricción única
**Categoría:** Bases de datos
**Definición:** Regla de la base que impide dos filas con los mismos valores en ciertas columnas. Garantiza que no haya dos reservas del mismo horario aunque lleguen al mismo tiempo; revisar en el código antes de insertar no lo garantiza.
