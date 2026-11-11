# Glosario - S03 - Secretos, pruebas y trabajo en equipo

> Definiciones de los términos técnicos de la sesión 3.
> Ordenado alfabéticamente.

---

## A

### AES-GCM
**Categoría:** Cifrado
**Definición:** Método de cifrado simétrico con autenticación: además de cifrar, detecta si los datos fueron alterados o si la clave es incorrecta.
**En el curso:** Es el `method "aes_gcm"` con el que OpenTofu cifra el estado y el plan; una frase equivocada falla con `cipher: message authentication failed`.

### assert
**Categoría:** Pruebas
**Definición:** Bloque dentro de un `run` de `tofu test` con una `condition` y un `error_message`. La condición debe ser verdadera para que la prueba pase y debe referirse a recursos, data sources, variables, salidas o módulos del código principal.
**En el curso:** `length(local_file.servicio) == 3`.

---

## C

### Cifrado de estado
**Categoría:** Seguridad
**Definición:** Función de OpenTofu que cifra el archivo de estado y los planes en reposo, configurada con un bloque `encryption`. No protege a quien ejecuta `tofu`, ni contra la pérdida de datos o la repetición de un estado viejo.
**En el curso:** Después del cifrado, `grep` ya no encuentra la contraseña en `terraform.tfstate`.

### command (en `run`)
**Categoría:** Pruebas
**Definición:** Dice si un paso de prueba hace `plan` (no crea nada) o `apply` (crea y luego destruye; es el valor por omisión).
**En el curso:** Cuatro `run` de plan y uno de apply.

---

## E

### enforced
**Categoría:** Seguridad
**Definición:** Opción de los bloques `state` y `plan` del cifrado que rechaza datos sin cifrar.
**En el curso:** Con un estado en claro, `init` falla con `encountered unencrypted payload without unencrypted method configured`.

### ephemeral
**Categoría:** Seguridad
**Definición:** Calificativo de un valor (variable, salida, recurso) que no se guarda en el estado ni en el plan y existe solo durante una ejecución.
**En el curso:** `ephemeral "random_password"`.

### expect_failures
**Categoría:** Pruebas
**Definición:** Lista, dentro de un `run`, de condiciones (validaciones, precondiciones, poscondiciones, checks) que **deben** fallar para que la prueba pase.
**En el curso:** `expect_failures = [var.entorno]`.

---

## F

### fallback
**Categoría:** Seguridad
**Definición:** Dentro de `state` o `plan`, método que OpenTofu prueba si no puede leer con el método nuevo. Al guardar siempre usa el método nuevo.
**En el curso:** Sirve para migrar un estado en claro al cifrado y se retira después.

### fmt
**Categoría:** Ciclo de trabajo
**Definición:** Comando que reescribe los archivos al formato canónico; con `-check` solo avisa (sale con código 3 si hay diferencias).
**En el curso:** Primer paso del pipeline del pull request.

---

## G

### GITHUB_STEP_SUMMARY
**Categoría:** CI
**Definición:** Archivo de GitHub Actions al que un paso agrega Markdown para mostrarlo como resumen del trabajo.
**En el curso:** El plan legible para quien revisa.

---

## K

### Key provider
**Categoría:** Seguridad
**Definición:** Componente del cifrado de OpenTofu que produce la clave a partir de una fuente. `pbkdf2` la deriva de una frase de al menos 16 caracteres.
**En el curso:** `key_provider "pbkdf2" "clave"`.

---

## M

### mock_provider
**Categoría:** Pruebas
**Definición:** Bloque de `tofu test` que reemplaza un proveedor por uno simulado, para probar sin crear recursos reales.
**En el curso:** Se menciona como la primera opción para recursos de nube.

---

## O

### override_resource
**Categoría:** Pruebas
**Definición:** Bloque de `tofu test` que evita la creación real de un recurso y genera valores para él.
**En el curso:** Se menciona junto a `mock_provider`.

---

## P

### Plan guardado (`-out`)
**Categoría:** Ciclo de trabajo
**Definición:** Archivo con el plan calculado, listo para aplicarse. Contiene la configuración y las variables, y puede incluir valores sensibles en texto plano si no se cifra.
**En el curso:** `p.plan` abre como zip con la contraseña dentro; `c.plan` cifrado, no.

### PBKDF2
**Categoría:** Cifrado
**Definición:** Función que deriva una clave criptográfica a partir de una frase, con muchas iteraciones para encarecer los intentos de adivinarla.
**En el curso:** El estado cifrado guarda en `meta` sus parámetros (iteraciones, función hash).

---

## R

### run
**Categoría:** Pruebas
**Definición:** Un paso de un archivo de prueba: sus variables, su comando y sus aserciones.
**En el curso:** Cada prueba del módulo es un `run`.

---

## S

### sensitive
**Categoría:** Seguridad
**Definición:** Argumento de variables y salidas que oculta el valor en la pantalla del plan y el apply. No lo quita del estado.
**En el curso:** El lema: el estado puede contener secretos en texto plano.

### setup-opentofu
**Categoría:** CI
**Definición:** Acción de GitHub que instala OpenTofu en un trabajo. Se fija por SHA y la versión de OpenTofu con `tofu_version`.
**En el curso:** v2.0.2 en el workflow de plan.

---

## T

### `TF_VAR_nombre`
**Categoría:** Variables
**Definición:** Variable de entorno con la que se da valor a la variable declarada `nombre`. Tiene la menor precedencia entre las fuentes de valores.
**En el curso:** `TF_VAR_clave_db` y `TF_VAR_passphrase`.

### tofu test
**Categoría:** Pruebas
**Definición:** Comando que ejecuta los archivos `.tftest.hcl` y sale con código distinto de cero si una prueba falla.
**En el curso:** Cinco pruebas sobre el módulo de la sesión 2.

---

## U

### unencrypted (método)
**Categoría:** Seguridad
**Definición:** Método de cifrado que no cifra; existe para migrar. Mientras esté configurado, OpenTofu avisa que es un riesgo.
**En el curso:** `method "unencrypted" "migrar" {}`.

---

## V

### validate
**Categoría:** Ciclo de trabajo
**Definición:** Comando que revisa sintaxis, tipos y referencias sin acceder a la plataforma.
**En el curso:** Detecta un argumento mal escrito (`contenido` en lugar de `content`).

---

## W

### Write-only attribute
**Categoría:** Seguridad
**Definición:** Atributo de un recurso administrado que acepta valores efímeros y nunca se guarda en el estado ni en el plan (se guarda como `null`). Requiere OpenTofu 1.11 o posterior; depende de que el proveedor lo ofrezca.
**En el curso:** Ni `hashicorp/local` ni `hashicorp/random` lo tienen en sus recursos administrados.
