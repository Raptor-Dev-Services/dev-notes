# Glosario - M03 - Red y segmentación

> Definiciones de los términos técnicos del módulo 3.
> Ordenado alfabéticamente.

---

## #

### 0.0.0.0/0
**Categoría:** Redes
**Definición:** Notación CIDR que representa todas las direcciones IPv4. En una regla de entrada significa "cualquier origen"; en una ruta, "todo destino que no tenga una ruta más específica".
**En el curso:** La regla que el auditor busca en SSH, RDP y puertos de bases de datos.

---

## A

### ACL de red (NACL)
**Categoría:** Redes / AWS
**Definición:** Lista de reglas numeradas aplicada a una subred. Permite y deniega, no tiene estado, se evalúa de menor a mayor número y termina en una regla `*` que deniega.
**En el curso:** El `deny 90` ganó al `allow 100` en la simulación.

---

### Application Security Group (ASG)
**Categoría:** Redes / Azure
**Definición:** Agrupación de interfaces por función de la aplicación, usable como origen o destino de reglas de un NSG sin escribir direcciones IP.
**En el curso:** Equivale, en espíritu, a referenciar un grupo de seguridad de AWS como origen.

---

## B

### Bastión
**Categoría:** Acceso
**Definición:** Servidor expuesto cuyo único trabajo es servir de salto para administrar otros. Es un servidor más que hay que parchear, vigilar y registrar.
**En el curso:** Se compara con Session Manager, que no abre puertos de entrada.

---

## C

### Con estado (stateful)
**Categoría:** Redes
**Definición:** Filtro que recuerda la conexión: si permite la ida, permite la respuesta sin regla adicional. Es el caso de los grupos de seguridad y de los NSG.
**En el curso:** Por eso un SG no necesita regla de retorno.

---

## D

### Defensa en profundidad
**Categoría:** Diseño
**Definición:** Principio de poner varios controles distintos para que el fallo de uno no abra todo. La red es una capa, junto con identidad, datos y detección.
**En el curso:** La primera lámina de la clase.

---

## E

### Egress (tráfico de salida)
**Categoría:** Redes
**Definición:** Tráfico que sale de tu red. Un grupo de seguridad nuevo permite toda la salida por omisión, por lo que filtrarla es una decisión explícita.
**En el curso:** La salida abierta se discutió como riesgo de exfiltración.

---

### Endpoint de interfaz (interface endpoint)
**Categoría:** Redes / AWS
**Definición:** Endpoint de VPC que crea una interfaz de red con IP privada en tu subred para llegar a un servicio por PrivateLink. Se resuelve por DNS y tiene cargo.
**En el curso:** Alternativa privada a llamar a un servicio por Internet.

---

### Endpoint de puerta de enlace (gateway endpoint)
**Categoría:** Redes / AWS
**Definición:** Endpoint de VPC para S3 y DynamoDB que funciona con una ruta a una lista de prefijos del servicio. No usa PrivateLink y no tiene cargo adicional.
**En el curso:** Permite a la subred de app hablar con `lab-datos` sin NAT.

---

## F

### Flow log
**Categoría:** Registro
**Definición:** Registro de metadatos del tráfico IP (origen, destino, puertos, protocolo, bytes, acción), sin el contenido de los paquetes. En AWS se publica en CloudWatch Logs, S3 o Data Firehose.
**En el curso:** Se usa para comprobar que un REJECT ocurre cuando se espera.

---

## G

### Grupo de seguridad (security group)
**Categoría:** Redes / AWS
**Definición:** Filtro con estado asociado a interfaces de red. Solo tiene reglas que permiten; todo lo demás se deniega. Se pueden referenciar otros grupos como origen.
**En el curso:** `sg-db` solo acepta el 5432 desde `sg-app`.

---

### Grupo de seguridad default
**Categoría:** Redes / AWS
**Definición:** Grupo que trae toda VPC: entrada solo desde sí mismo y salida abierta. No se borra, pero se puede cambiar; si no indicas grupo, se asigna.
**En el curso:** Primer lugar donde se buscan recursos que nadie configuró.

---

## I

### Internet Gateway (IGW)
**Categoría:** Redes / AWS
**Definición:** Componente que permite a una VPC comunicarse con Internet. Una subred es pública por tener una ruta hacia él.
**En el curso:** Definición operativa de "subred pública".

---

## N

### NAT Gateway
**Categoría:** Redes / AWS
**Definición:** Permite a recursos de una subred privada iniciar conexiones hacia afuera sin ser alcanzables desde afuera.
**En el curso:** Se contrasta con la subred de datos, que no tiene ruta.

---

### Network Security Group (NSG)
**Categoría:** Redes / Azure
**Definición:** Filtro con estado para tráfico entre recursos de Azure. Reglas con prioridad de 100 a 4096, que permiten o deniegan; trae reglas por defecto de prioridad 65000 a 65500.
**En el curso:** El NSG del fragmento de Bicep abría el 22 a todos.

---

## P

### Port forwarding
**Categoría:** Acceso
**Definición:** Reenvío de un puerto remoto a uno local a través de una sesión. En Session Manager, esas sesiones no registran los comandos.
**En el curso:** Limitación señalada al hablar de acceso administrativo.

---

### Private Endpoint
**Categoría:** Redes / Azure
**Definición:** Interfaz de red con IP privada de tu red virtual que conecta con un servicio de Private Link. Requiere DNS; no restringe por sí solo el acceso público del servicio.
**En el curso:** Error típico: crearlo y dejar el acceso público abierto.

---

### PrivateLink
**Categoría:** Redes
**Definición:** Tecnología para acceder a servicios por direcciones privadas, con tráfico que no sale a Internet.
**En el curso:** Base de los endpoints de interfaz de AWS y de Private Link en Azure.

---

### Puerto efímero
**Categoría:** Redes
**Definición:** Puerto de origen temporal que elige el cliente. Una NACL sin estado necesita reglas que permitan el rango de retorno.
**En el curso:** La respuesta del 443 murió por no permitirse.

---

## S

### Segmentación
**Categoría:** Diseño
**Definición:** Dividir una red en zonas con controles entre ellas, para limitar hasta dónde llega un compromiso.
**En el curso:** Entre app y datos, entre entornos y entre cuentas.

---

### Service tag
**Categoría:** Redes / Azure
**Definición:** Etiqueta que agrupa los prefijos de IP de un servicio de Azure, para usarla en reglas sin mantener listas.
**En el curso:** Concepto vecino de las listas de prefijos de AWS.

---

### Session Manager
**Categoría:** Acceso / AWS
**Definición:** Capacidad de AWS Systems Manager para abrir sesiones en nodos administrados sin puertos de entrada, bastiones ni llaves SSH, con control por IAM y registro de sesiones.
**En el curso:** Sustituto del SSH abierto a un rango.

---

### Sin estado (stateless)
**Categoría:** Redes
**Definición:** Filtro que no recuerda conexiones: la ida y la vuelta se evalúan por separado. Es el caso de las NACL.
**En el curso:** El error de "entra pero no responde".

---

### Subred privada / de datos
**Categoría:** Redes
**Definición:** Subred sin ruta directa a un Internet Gateway; la de datos, además, sin salida por NAT.
**En el curso:** Donde vive `db-lab`.

---

### Superficie de ataque
**Categoría:** Riesgo
**Definición:** Conjunto de puntos por donde algo puede ser alcanzado. Cada regla abierta, puerto escuchando o endpoint público la amplía.
**En el curso:** Se reduce cerrando reglas, no solo vigilándolas.

---

## V

### VPC (Virtual Private Cloud)
**Categoría:** Redes / AWS
**Definición:** Red virtual aislada dentro de AWS, con su rango CIDR, subredes, rutas y controles de tráfico.
**En el curso:** `10.20.0.0/16` del Taller Demo.

---
