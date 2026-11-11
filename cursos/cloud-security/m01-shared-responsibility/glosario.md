# Glosario - M01 - Responsabilidad compartida y activos

> Definiciones de los términos técnicos del módulo 1. Ordenado alfabéticamente.

---

## A

### Activo
**Categoría:** Gestión de activos
**Definición:** Cualquier recurso que tiene valor y se debe proteger: una máquina virtual, un bucket, una llave de cifrado, un secreto, un pipeline, un registro DNS.
**En el curso:** El inventario de la práctica lista activos con dueño y clasificación.

---

### AWS Config
**Categoría:** AWS / Gobernanza
**Definición:** Servicio que da una vista detallada de la configuración de los recursos de una cuenta, de cómo se relacionan y de cómo cambiaron. Graba solo los tipos de recurso que se elijan y permite reglas que marcan lo no conforme.
**En el curso:** Es la herramienta de inventario continuo en AWS; en el módulo 6 se usa para cumplimiento.

---

## B

### Block Public Access
**Categoría:** AWS / S3
**Definición:** Conjunto de cuatro ajustes (`BlockPublicAcls`, `IgnorePublicAcls`, `BlockPublicPolicy`, `RestrictPublicBuckets`) que limitan el acceso público a buckets, puntos de acceso, cuentas u organizaciones, y que prevalecen sobre las políticas y ACL.
**En el curso:** Se activan los cuatro en el bucket y se verifica con `get_public_access_block`.

---

## C

### Clasificación de datos
**Categoría:** Protección de datos
**Definición:** Asignar a cada conjunto de datos un nivel según su sensibilidad, de modo que cada nivel tenga reglas mínimas de protección.
**En el curso:** Cuatro niveles ilustrativos: pública, interna, confidencial, restringida.

---

### Control compartido
**Categoría:** Responsabilidad compartida
**Definición:** Según AWS, control que aplica tanto a la capa de infraestructura como a la del cliente, y cada parte implementa lo suyo (por ejemplo, gestión de parches).
**En el curso:** Se contrasta con el control heredado.

---

### Control heredado
**Categoría:** Responsabilidad compartida
**Definición:** Control que el cliente hereda por completo del proveedor. AWS da como ejemplo los controles físicos y ambientales.
**En el curso:** Es lo único que "ya viene hecho"; no incluye datos ni identidades.

---

## D

### Defensa en profundidad
**Categoría:** Principios
**Definición:** Usar varias capas de control, de modo que la falla de una no deje todo expuesto.
**En el curso:** Es la razón de que el curso sea una cadena de seis módulos.

---

## E

### Etiqueta (tag)
**Categoría:** Gestión de activos
**Definición:** Par clave-valor que se adjunta a un recurso para organizarlo, cobrarlo, automatizarlo o controlar el acceso. En AWS el máximo es de 50 etiquetas de usuario por recurso; en Azure, 50 pares.
**En el curso:** `taller:owner` y `taller:clasificacion`.

---

## G

### Gobernanza reactiva y proactiva
**Categoría:** Gobernanza
**Definición:** La reactiva encuentra lo que ya existe sin cumplir la convención; la proactiva impide crearlo mal.
**En el curso:** En el módulo 1 se hace la reactiva con un script; en el 6 se automatiza.

---

## I

### IaaS, PaaS, SaaS
**Categoría:** Modelos de servicio
**Definición:** Infraestructura, plataforma y software como servicio. Cuanto más abstracto el servicio, más administra el proveedor; datos, identidades y configuración siguen siendo del cliente.
**En el curso:** `api-lab` es IaaS; `db-lab`, PaaS.

---

### Inventario
**Categoría:** Gestión de activos
**Definición:** Lista de activos con, al menos, qué son, de quién son, qué tan delicados son y cómo están expuestos.
**En el curso:** Se produce como CSV y se mide antes y después de corregir.

---

## M

### moto
**Categoría:** Herramienta de laboratorio
**Definición:** Biblioteca y servidor de Python que simula la API de AWS en la máquina local. Implementa un subconjunto del comportamiento real.
**En el curso:** Es la "nube" de las prácticas: sin cuenta, sin costo.

---

## R

### Resource Graph (Azure)
**Categoría:** Azure / Gobernanza
**Definición:** Servicio que consulta recursos a escala entre suscripciones con un lenguaje basado en KQL. No es fuertemente consistente.
**En el curso:** Es el equivalente de la consulta de inventario en Azure.

---

### Responsabilidad compartida
**Categoría:** Principios
**Definición:** Modelo en el que el proveedor protege la infraestructura de la nube y el cliente protege lo que pone en ella; la línea depende del servicio.
**En el curso:** Es la pregunta de partida de los seis módulos.

---

## S

### SSE-S3
**Categoría:** AWS / Cifrado
**Definición:** Cifrado del lado del servidor con llaves administradas por S3 (AES-256). Desde el 5 de enero de 2023 se aplica a todo objeto nuevo.
**En el curso:** Es el piso del cifrado; no sustituye controlar quién usa las llaves.
