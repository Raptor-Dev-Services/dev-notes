# Glosario - S01 - Por qué IaC, y el estado

> Definiciones de los términos técnicos de la sesión 1.
> Ordenado alfabéticamente.

---

## A

### Advisory lock
**Categoría:** Bases de datos / Bloqueo
**Definición:** Bloqueo que PostgreSQL ofrece a las aplicaciones: la base no lo impone sobre los datos, solo guarda quién lo tiene, y lo libera solo cuando termina la sesión o se pierde la conexión.
**En el curso:** El backend `pg` de OpenTofu lo usa para bloquear el estado; por eso la documentación dice que `force-unlock` no está soportado en ese backend.

### apply
**Categoría:** Ciclo de trabajo
**Definición:** Comando que ejecuta los cambios de un plan. Sin un plan guardado, calcula uno y pide aprobación; con un plan guardado, pasarlo cuenta como aprobación.
**En el curso:** `tofu apply -auto-approve` crea el primer recurso; con `-replace` fuerza el recurso lento de la práctica.

---

## B

### Backend
**Categoría:** Estado
**Definición:** Componente que decide dónde se guarda el estado y cómo se protege. El local guarda un archivo; otros guardan en una base de datos o en almacenamiento de objetos. No todos soportan bloqueo.
**En el curso:** `backend "pg" {}` guarda el estado en una tabla de PostgreSQL 17.

### Bloqueo del estado (state lock)
**Categoría:** Estado
**Definición:** Mecanismo que impide que dos operaciones escriban el estado al mismo tiempo. OpenTofu lo toma automáticamente en toda operación que podría escribirlo.
**En el curso:** Dos `apply` simultáneos: el segundo falla con `Error acquiring the state lock`.

### BSL (Business Source License)
**Categoría:** Licencias
**Definición:** Licencia a la que HashiCorp anunció, el 10 de agosto de 2023, que cambiaría el código de sus productos en versiones futuras, antes con MPL 2.0.
**En el curso:** Es el origen de OpenTofu.

---

## C

### Cadena de conexión (`conn_str`)
**Categoría:** Backend pg
**Definición:** Texto con el que el backend `pg` se conecta a PostgreSQL. Puede ir en el bloque o en la variable de entorno `PG_CONN_STR`; si lleva contraseña conviene la segunda.
**En el curso:** `postgres://postgres:...@127.0.0.1:15432/estado?sslmode=disable`.

---

## D

### Data source
**Categoría:** Lenguaje
**Definición:** Bloque que **lee** información de algo que ya existe, sin crearlo ni administrarlo.
**En el curso:** Se nombra en el vocabulario mínimo; se usa a fondo en la sesión 2.

### Declarativo
**Categoría:** Conceptos
**Definición:** Estilo en el que se describe el **resultado** deseado y la herramienta calcula los pasos. Ejecutarlo dos veces da el mismo resultado.
**En el curso:** Contrasta con el script de `mkdir` que falla la segunda vez.

### destroy
**Categoría:** Ciclo de trabajo
**Definición:** Plan inverso: borra los recursos que el estado dice que se crearon. Si el estado se perdió, no sabe qué borrar.
**En el curso:** Limpieza al final de la práctica.

### Drift
**Categoría:** Estado
**Definición:** Diferencia entre lo que el estado cree y lo que existe en la plataforma, porque alguien cambió algo por fuera de la herramienta.
**En el curso:** Se provoca editando `salida/nota.txt` a mano y leyéndolo en el plan.

---

## E

### Estado (state)
**Categoría:** Estado
**Definición:** Registro, en JSON, que mapea los recursos de tu configuración con los del mundo real y guarda sus atributos. Por omisión es el archivo local `terraform.tfstate`. Puede contener secretos en texto plano.
**En el curso:** Es "la memoria del sistema" del lema de la sesión.

---

## F

### Fork
**Categoría:** Software libre
**Definición:** Copia de un proyecto que continúa por su cuenta con su propio gobierno.
**En el curso:** OpenTofu es una bifurcación de Terraform, según su FAQ.

### force-unlock
**Categoría:** Estado
**Definición:** Comando que libera un bloqueo del estado dado su ID. La documentación indica usarlo solo para quitar tu propio bloqueo cuando el desbloqueo automático falló.
**En el curso:** Se explica por qué forzarlo sin preguntar puede dejar a dos escritores trabajando a la vez.

---

## H

### HCL
**Categoría:** Lenguaje
**Definición:** Lenguaje de configuración con el que se escriben los archivos `.tf`: bloques, argumentos y expresiones.
**En el curso:** Todos los ejemplos están en HCL.

---

## I

### IaC (infraestructura como código)
**Categoría:** Conceptos
**Definición:** Práctica de definir y administrar infraestructura con archivos versionables en lugar de pasos manuales.
**En el curso:** Es el tema del curso completo.

### Imperativo
**Categoría:** Conceptos
**Definición:** Estilo en el que se escriben los pasos a seguir, en orden.
**En el curso:** El script de shell frente al recurso declarativo.

### import
**Categoría:** Estado
**Definición:** Bloque (o comando) que asocia un recurso real con una dirección de tu código en el estado. Se procesa durante el plan y se aplica al aprobar.
**En el curso:** Se importa un volumen de Docker por su nombre.

### init
**Categoría:** Ciclo de trabajo
**Definición:** Comando que prepara la carpeta: instala proveedores, configura el backend y escribe el archivo de bloqueo de dependencias.
**En el curso:** Se repite al cambiar de backend o de proveedores.

---

## L

### Lineage
**Categoría:** Estado
**Definición:** Identificador único de la historia de un estado. Dos estados con distinto `lineage` no son el mismo.
**En el curso:** Aparece en `terraform.tfstate` junto a `serial`.

### `-lock-timeout`
**Categoría:** Opciones de comandos
**Definición:** Opción que hace que OpenTofu reintente obtener el bloqueo durante un tiempo (número y unidad, como `3s`) antes de dar error.
**En el curso:** `-lock-timeout=5s` espera 5 segundos y falla porque el otro apply dura 25.

### Lock file (`.terraform.lock.hcl`)
**Categoría:** Proveedores
**Definición:** Archivo que `init` escribe con las versiones exactas de los proveedores y sus huellas. Se versiona.
**En el curso:** Fija `hashicorp/local` en 2.9.1 aunque la restricción sea `~> 2.5`.

---

## M

### Migrar el estado
**Categoría:** Estado
**Definición:** Copiar un estado existente a otro backend. En `init`, con `-migrate-state`.
**En el curso:** Se explica como fragmento.

---

## O

### OpenTofu
**Categoría:** Herramientas
**Definición:** Herramienta de IaC de código abierto, bifurcada de Terraform. Su comando es `tofu` y su repositorio usa MPL-2.0.
**En el curso:** La herramienta de todo el curso.

### Output
**Categoría:** Lenguaje
**Definición:** Valor que una configuración publica al terminar, para mostrarlo o para que otra configuración lo use.
**En el curso:** `ruta` muestra el archivo creado.

---

## P

### plan
**Categoría:** Ciclo de trabajo
**Definición:** Comando que calcula la diferencia entre el código y lo que existe, y la muestra sin cambiar nada. Con `-out` se guarda.
**En el curso:** Se aprende a leer `+`, `-`, `~` y `-/+`.

### Plan guardado
**Categoría:** Ciclo de trabajo
**Definición:** Archivo de salida de `plan -out`. Contiene la configuración completa y las variables, por lo que puede incluir valores sensibles en texto plano.
**En el curso:** `cambio.plan` y `tofu show`.

### Provider
**Categoría:** Lenguaje
**Definición:** Plugin que permite a la herramienta administrar un tipo de plataforma. Se descarga en `init` de un registro.
**En el curso:** `hashicorp/local` y `kreuzwerker/docker`.

---

## R

### Recurso (resource)
**Categoría:** Lenguaje
**Definición:** Bloque que declara algo que la herramienta crea y administra.
**En el curso:** `local_file`, `terraform_data`, `docker_volume`.

### `-refresh-only`
**Categoría:** Opciones de comandos
**Definición:** Modo que actualiza el estado y las salidas para reflejar cambios hechos fuera de la herramienta, sin tocar la infraestructura.
**En el curso:** Se usa para aceptar un volumen borrado a mano.

### Reemplazo
**Categoría:** Ciclo de trabajo
**Definición:** Cambio que obliga a destruir y volver a crear un recurso; se marca `-/+` y `forces replacement`.
**En el curso:** Cambiar `content` de `local_file`.

### Registro (registry)
**Categoría:** Proveedores
**Definición:** Servicio de donde se descargan proveedores. OpenTofu usa `registry.opentofu.org`, distinto del de Terraform.
**En el curso:** Se ve en la dirección del proveedor dentro del estado.

---

## S

### Serial
**Categoría:** Estado
**Definición:** Número que aumenta con cada cambio del estado.
**En el curso:** Aparece en el JSON del estado.

---

## T

### `terraform_data`
**Categoría:** Recursos integrados
**Definición:** Recurso integrado que no administra nada externo; sirve para ejecutar provisioners o guardar valores.
**En el curso:** `terraform_data.lento` ejecuta `sleep 25` para abrir la ventana de la carrera.

---

## V

### Variable
**Categoría:** Lenguaje
**Definición:** Entrada de una configuración, con tipo y, opcionalmente, valor por omisión.
**En el curso:** `var.saludo`.

---

## W

### Workspace
**Categoría:** Estado
**Definición:** Estado nombrado dentro de un mismo backend. Sin workspaces se usa `default`.
**En el curso:** El backend `pg` guarda una fila por workspace; la sesión 2 compara workspaces con carpetas.
