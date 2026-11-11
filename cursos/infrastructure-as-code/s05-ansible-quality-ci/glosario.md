# Glosario - S05 - Calidad y CI para Ansible

> Definiciones de los términos técnicos de la sesión 5.
> Ordenado alfabéticamente.

---

## A

### ansible-lint
**Categoría:** Herramienta
**Definición:** Linter para playbooks, roles y colecciones. Revisa el contenido contra reglas y dice qué regla rompe cada línea. Sale con código distinto de cero si hay hallazgos.
**En el curso:** Con perfil `production` en el workflow; salida real con código 2 al encontrar `no-changed-when`.

---

### ansible-vault
**Categoría:** Herramienta
**Definición:** Herramienta que cifra archivos o valores individuales para guardar secretos junto al código. Se necesita la clave del vault para usarlos.
**En el curso:** `ansible-vault encrypt_string --vault-password-file .vault-pass --stdin-name db_password`.

---

## B

### Buenas prácticas
**Categoría:** Principios
**Definición:** Conjunto de hábitos para que los roles sean legibles y mantenibles: nombres claros, FQCN, módulos antes que `command`, variables con prefijo y verificación incluida.
**En el curso:** Capítulo 24 de *Ansible: Up and Running* y las reglas de ansible-lint.

---

## C

### Cleaning up
**Categoría:** Molecule
**Definición:** Mensaje de Molecule cuando una etapa falla: ejecuta `cleanup` y `destroy` para no dejar el servidor de prueba vivo.
**En el curso:** Tras el fallo de idempotencia, el contenedor se borra solo.

---

### community.docker
**Categoría:** Colección
**Definición:** Colección de módulos y plugins para Docker. Su plugin de conexión `community.docker.docker` ejecuta módulos dentro de un contenedor.
**En el curso:** Conecta Molecule al contenedor `a07-molecule`; la versión probada es 5.4.0.

---

### converge
**Categoría:** Molecule
**Definición:** Etapa de Molecule que aplica el rol al servidor de prueba (`converge.yml`).
**En el curso:** Primera aplicación del rol: `ok=15 changed=9`.

---

### create / destroy
**Categoría:** Molecule
**Definición:** Etapas que levantan y borran el servidor de prueba. En Molecule 26 las escribe el autor del escenario.
**En el curso:** `create.yml` usa `community.docker.docker_container`.

---

## D

### debug (módulo)
**Categoría:** Depuración
**Definición:** Módulo `ansible.builtin.debug` que imprime una variable (`var`) o un mensaje (`msg`).
**En el curso:** Mostrar `puertos` o el `PRETTY_NAME` del sistema.

---

### dependency
**Categoría:** Molecule
**Definición:** Etapa que instala las colecciones y roles que el escenario necesita.
**En el curso:** Lee `requirements.yml` del escenario.

---

## E

### Escenario
**Categoría:** Molecule
**Definición:** Carpeta `molecule/<nombre>/` con la configuración y los playbooks de una prueba. El predeterminado se llama `default`.
**En el curso:** `roles/endurecer/molecule/default/`.

---

## F

### FRAGMENTO
**Categoría:** Convención del curso
**Definición:** Marca que indica código que no se ejecutó tal cual en el curso, por ejemplo un workflow que no corrió en un runner real.
**En el curso:** El workflow `rol-endurecer.yml` está marcado como fragmento: se validó con un parser.

---

## I

### idempotence (etapa)
**Categoría:** Molecule
**Definición:** Etapa que vuelve a aplicar el rol y falla si cualquier tarea reporta un cambio.
**En el curso:** Detectó `Registrar los rechazos en el log` con `ufw logging low`.

---

### include_role
**Categoría:** Roles
**Definición:** Forma dinámica de usar un rol desde `tasks`; se procesa al ejecutarse.
**En el curso:** `converge.yml` aplica `raptor_academy.endurecer`.

---

### Instancia
**Categoría:** Molecule
**Definición:** Servidor de prueba de un escenario. Aquí, un contenedor `ubuntu:24.04`.
**En el curso:** Grupo `instance` del inventario del escenario.

---

## L

### --list-tasks / --list-tags
**Categoría:** Depuración
**Definición:** Opciones de `ansible-playbook` que listan las tareas o las etiquetas sin ejecutar nada.
**En el curso:** Para ver el plan antes de correr.

---

### Linter
**Categoría:** Calidad
**Definición:** Programa que revisa código contra reglas sin ejecutarlo.
**En el curso:** ansible-lint para Ansible.

---

## M

### Molecule
**Categoría:** Herramienta
**Definición:** Framework para probar roles, colecciones y playbooks de Ansible creando servidores de prueba y ejecutando una secuencia de etapas.
**En el curso:** Versión 26.11.0 del curso.

---

## N

### namespace (de un rol)
**Categoría:** Roles
**Definición:** Espacio de nombres de Galaxy que, junto al nombre del rol, forma el nombre completo `namespace.rol`. Se declara en `meta/main.yml`.
**En el curso:** `raptor_academy.endurecer`.

---

### no_log
**Categoría:** Seguridad
**Definición:** Palabra clave que oculta el resultado de una tarea en la salida y en los errores. Protege secretos y también esconde el diagnóstico.
**En el curso:** Con el vault sin clave, el error sale como `censored`.

---

### noqa
**Categoría:** Calidad
**Definición:** Comentario `# noqa: regla` al final de una línea para que ansible-lint omita esa regla ahí. Debe llevar el motivo.
**En el curso:** Se evita; si se usa, con justificación.

---

## P

### Perfil (de ansible-lint)
**Categoría:** Calidad
**Definición:** Nivel de exigencia que agrupa reglas: `min`, `basic`, `moderate`, `safety`, `shared` y `production`; cada uno incluye al anterior.
**En el curso:** Se usa `--profile production`.

---

### prepare
**Categoría:** Molecule
**Definición:** Etapa opcional para dejar lista la imagen base antes de aplicar el rol.
**En el curso:** Instala Python, sudo y OpenSSH con `raw`.

---

## R

### raw (módulo)
**Categoría:** Módulos
**Definición:** Ejecuta un comando por SSH sin necesitar Python en el destino.
**En el curso:** Instalar Python en la imagen base.

---

### Regla (de lint)
**Categoría:** Calidad
**Definición:** Comprobación con identificador (`no-changed-when`, `name[casing]`, `fqcn`) que ansible-lint aplica al contenido.
**En el curso:** El lint dice regla, archivo y línea.

---

## S

### --start-at-task / --step
**Categoría:** Depuración
**Definición:** Opciones de `ansible-playbook`: empezar en la tarea con ese nombre o confirmar cada tarea antes de ejecutarla.
**En el curso:** Retomar sin repetir las 40 primeras tareas.

---

### --syntax-check
**Categoría:** Depuración
**Definición:** Valida la sintaxis del playbook sin ejecutarlo.
**En el curso:** Detecta el YAML mal sangrado.

---

### SHA fijado (de una acción)
**Categoría:** CI
**Definición:** Referencia a una acción de GitHub por el hash exacto de su commit, con el tag en un comentario, para que una versión no cambie sin avisar.
**En el curso:** `actions/checkout@3d3c42e5...  # v7.0.1`.

---

## V

### Vault ID / clave del vault
**Categoría:** Seguridad
**Definición:** Contraseña con la que se cifra y descifra. La clave nunca va al repositorio.
**En el curso:** `.vault-pass` en `.gitignore`.

---

### verify
**Categoría:** Molecule
**Definición:** Etapa que ejecuta `verify.yml` para comprobar el estado final del servidor.
**En el curso:** `assert` sobre `sshd -T`, UFW y la jaula de fail2ban.

---

## W

### Workflow
**Categoría:** CI
**Definición:** Archivo YAML de GitHub Actions que define cuándo y qué corre el CI.
**En el curso:** `.github/workflows/rol-endurecer.yml`.

---

