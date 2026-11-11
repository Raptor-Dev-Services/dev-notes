# Glosario - S02 - Módulos y entornos

> Definiciones de los términos técnicos de la sesión 2.
> Ordenado alfabéticamente.

---

## C

### count
**Categoría:** Meta-argumentos
**Definición:** Argumento que crea un número entero de instancias de un recurso o módulo. Cada instancia se identifica por su posición (`count.index`, desde 0). Si derivas el valor de la longitud de una lista, quitar un elemento del medio desplaza a los demás.
**En el curso:** `count = length(var.servicios)` propone `3 to destroy` al quitar `"api"`; `count = condición ? 1 : 0` es el patrón del recurso opcional.

### Condicional
**Categoría:** Lenguaje
**Definición:** Expresión `condición ? si_verdadero : si_falso`.
**En el curso:** Activa el recurso `monitoreo` solo en staging.

---

## D

### Diferencias declaradas
**Categoría:** Práctica
**Definición:** Las diferencias entre entornos escritas como valores de entrada (un archivo `tfvars` por entorno), en lugar de código copiado y modificado.
**En el curso:** `diff entornos/dev/terraform.tfvars entornos/staging/terraform.tfvars` muestra todas.

### `dynamic`
**Categoría:** Lenguaje
**Definición:** Bloque que genera bloques anidados repetibles a partir de una colección. La documentación advierte que abusar de él dificulta la lectura.
**En el curso:** Genera los bloques `ports` de un contenedor a partir de un mapa.

---

## E

### each.key / each.value
**Categoría:** Meta-argumentos
**Definición:** Dentro de un recurso con `for_each`, la clave y el valor de la instancia actual. En un conjunto, ambos son el mismo elemento.
**En el curso:** `each.key` forma el nombre del archivo.

### Entorno
**Categoría:** Práctica
**Definición:** Copia de la infraestructura con un propósito: dev, staging, producción.
**En el curso:** Una carpeta por entorno, cada una con su estado.

### Expresión `for`
**Categoría:** Lenguaje
**Definición:** Construye una lista (con `[ ]`) o un mapa (con `{ }`) transformando y filtrando otra colección.
**En el curso:** Construye la salida del módulo como un mapa por nombre.

---

## F

### for_each
**Categoría:** Meta-argumentos
**Definición:** Argumento que crea una instancia por cada elemento de un mapa o conjunto de cadenas; cada una se identifica por su clave. El valor debe ser conocido antes de actuar sobre la plataforma y no puede ser sensible.
**En el curso:** Quitar un servicio destruye solo el suyo.

---

## L

### Local
**Categoría:** Lenguaje
**Definición:** Valor con nombre, interno a un módulo, que se calcula una vez y se reutiliza.
**En el curso:** `local.etiquetas`.

### Lock file
**Categoría:** Proveedores
**Definición:** `.terraform.lock.hcl`: guarda la versión exacta de cada proveedor y sus huellas (`zh:` y `h1:`). Se versiona.
**En el curso:** Se crea para varias plataformas con `tofu providers lock`.

---

## M

### Módulo
**Categoría:** Lenguaje
**Definición:** Carpeta de archivos `.tf` que otras configuraciones llaman con valores. Su frontera son sus variables y sus salidas.
**En el curso:** `modulos/servicios`.

### `moved`
**Categoría:** Refactorización
**Definición:** Bloque con `from` y `to` que dice que un objeto cambió de dirección, para que se conserve en lugar de destruirse y crearse. La documentación recomienda conservarlo en módulos compartidos.
**En el curso:** Pasa `servicio[0]` a `servicio["api"]` sin tocar la infraestructura.

---

## O

### Output (de un módulo)
**Categoría:** Lenguaje
**Definición:** Valor que un módulo publica. Es lo único de sus recursos internos que el que llama puede leer.
**En el curso:** `module.servicios.archivos`.

---

## P

### Precondition / postcondition
**Categoría:** Lenguaje
**Definición:** Comprobaciones dentro de `lifecycle`: la precondición verifica un supuesto antes de evaluar el objeto; la poscondición, una garantía después de planearlo o aplicarlo.
**En el curso:** Se mencionan junto a `validation`.

---

## R

### Registro de módulos
**Categoría:** Módulos
**Definición:** Servicio donde se publican módulos con versión. Es la única fuente de módulos que admite el argumento `version`.
**En el curso:** Se compara con Git y rutas locales.

---

## S

### source
**Categoría:** Módulos
**Definición:** Argumento obligatorio de un bloque `module` que dice de dónde viene el código: ruta local, registro, Git, HTTP, S3, entre otros.
**En el curso:** `source = "../../modulos/servicios"`.

### `state mv`
**Categoría:** Estado
**Definición:** Comando que mueve un objeto a otra dirección dentro del estado. Cambia solo el estado de quien lo corre y no queda en Git.
**En el curso:** Alternativa puntual a `moved`.

---

## T

### `terraform.tfvars`
**Categoría:** Variables
**Definición:** Archivo que OpenTofu carga solo, en el directorio actual, para dar valores a variables.
**En el curso:** Uno por entorno.

### `terraform.workspace`
**Categoría:** Estado
**Definición:** Expresión que da el nombre del workspace activo.
**En el curso:** Se usa en el ejemplo de workspaces.

---

## V

### Validación (`validation`)
**Categoría:** Lenguaje
**Definición:** Bloque dentro de una variable con `condition` y `error_message`. Rechaza valores inválidos en el plan, antes de actuar.
**En el curso:** Solo `dev`, `staging` o `prod` son entornos válidos.

### Versión de un módulo
**Categoría:** Módulos
**Definición:** Identificador de una revisión concreta de un módulo: `version` en un registro, `?ref=` en Git.
**En el curso:** `?ref=v1.0.0`.

---

## W

### Workspace
**Categoría:** Estado
**Definición:** Estado con nombre dentro de un mismo backend; `default` no se puede borrar. No son apropiados cuando hacen falta credenciales o controles de acceso separados.
**En el curso:** `tofu workspace new staging`.
