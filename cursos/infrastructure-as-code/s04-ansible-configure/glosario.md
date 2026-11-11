# Glosario - S04 - Ansible: configurar lo que se creó

> Definiciones de los términos técnicos de la sesión 4.
> Ordenado alfabéticamente.

---

## A

### Agentless
**Categoría:** Arquitectura
**Definición:** Modelo en el que la herramienta no instala ningún programa residente en el servidor administrado. Ansible se conecta por SSH, copia un módulo, lo ejecuta con Python y recoge el resultado.
**En el curso:** Solo hacen falta SSH y Python en el servidor. Por eso `ansible -m ping` es la primera comprobación.

---

### Ansible
**Categoría:** Herramienta
**Definición:** Herramienta de automatización de configuración que aplica un estado deseado a servidores mediante playbooks escritos en YAML.
**En el curso:** Configura por dentro la VM que Terraform u OpenTofu creó.

---

### ansible-core
**Categoría:** Herramienta
**Definición:** Paquete de PyPI con el motor de Ansible y la colección `ansible.builtin`. El paquete `ansible` suma además un conjunto grande de colecciones.
**En el curso:** El curso fija `ansible-core==2.21.6` porque 2.22 exige Python 3.13 y la VM trae 3.12.

---

### ansible-galaxy
**Categoría:** Herramienta
**Definición:** Comando para crear el esqueleto de un rol (`role init`) e instalar colecciones y roles desde un `requirements.yml`.
**En el curso:** `ansible-galaxy role init roles/endurecer` y `ansible-galaxy collection install -r requirements.yml`.

---

### ansible.cfg
**Categoría:** Configuración
**Definición:** Archivo de configuración de Ansible. En un proyecto fija el inventario, la ruta de roles y colecciones y opciones de SSH.
**En el curso:** Lleva `inventory`, `roles_path`, `collections_path` y `pipelining`.

---

### assert
**Categoría:** Módulo
**Definición:** Módulo `ansible.builtin.assert` que falla la ejecución si una condición no se cumple, con un mensaje propio.
**En el curso:** Comprueba que `sshd -T` muestra `passwordauthentication no`; atrapa el archivo `99-`.

---

## B

### become
**Categoría:** Privilegios
**Definición:** Palabra clave que sube privilegios en el servidor administrado, normalmente con sudo.
**En el curso:** `become: true` en el play del rol; `-K` pide la contraseña de sudo.

---

## C

### changed_when
**Categoría:** Idempotencia
**Definición:** Palabra clave que le dice a Ansible cuándo una tarea cuenta como cambio. `changed_when: false` marca que la tarea solo lee.
**En el curso:** Se usa en `sshd -T` para que la segunda corrida no reporte un cambio falso.

---

### Check mode
**Categoría:** Ejecución
**Definición:** Modo `--check`: simula la ejecución sin cambiar nada. Los módulos que no lo soportan no hacen ni reportan nada.
**En el curso:** `ansible-playbook idempotente.yml --check --diff` antes de aplicar.

---

### Colección
**Categoría:** Herramienta
**Definición:** Paquete que distribuye módulos, plugins y roles bajo un nombre `espacio.colección`, con versión.
**En el curso:** `community.general` 14.0.1 trae `community.general.ufw`; `ansible-core` no.

---

### creates
**Categoría:** Idempotencia
**Definición:** Argumento de `command` que salta la tarea si el archivo indicado ya existe.
**En el curso:** Alternativa para un `command` inevitable que debe correr una sola vez.

---

## D

### defaults (de un rol)
**Categoría:** Variables
**Definición:** Carpeta `defaults/main.yml` del rol con valores de muy baja precedencia: lo que quien usa el rol puede sobrescribir.
**En el curso:** Todas las variables `endurecer_*` y sus valores por omisión.

---

### Diff mode
**Categoría:** Ejecución
**Definición:** Opción `--diff`: muestra las diferencias de archivos y plantillas que se cambiarían o cambiaron.
**En el curso:** En tareas sensibles se apaga con `diff: false` porque puede mostrar secretos.

---

### Drop-in
**Categoría:** Linux
**Definición:** Archivo pequeño en un directorio `*.d` que se suma a la configuración principal sin editarla. En SSH, `/etc/ssh/sshd_config.d/*.conf`.
**En el curso:** `10-endurecer.conf`; OpenSSH usa el primer valor obtenido de cada opción.

---

## E

### Extra vars
**Categoría:** Variables
**Definición:** Variables pasadas con `-e` en la línea de comandos. Tienen la precedencia más alta (nivel 22).
**En el curso:** `-e endurecer_ssh_dropin=99-endurecer.conf` para provocar el fallo.

---

## F

### Facts
**Categoría:** Variables
**Definición:** Datos que Ansible descubre del servidor con el módulo `setup` al empezar un play: sistema, versión, red, hardware.
**En el curso:** `ansible_distribution_release` vale `noble` en Ubuntu 24.04.

---

### fail2ban
**Categoría:** Seguridad
**Definición:** Servicio que lee logs y bloquea temporalmente las IP que fallan demasiadas veces. Se configura por jaulas (*jails*).
**En el curso:** Jaula `sshd` con `maxretry`, `findtime` y `bantime` como variables del rol.

---

### flush_handlers
**Categoría:** Handlers
**Definición:** Tarea `meta` que ejecuta de inmediato los handlers pendientes en lugar de esperar al final del play.
**En el curso:** Recarga SSH antes de comprobar la configuración efectiva.

---

### FQCN
**Categoría:** Módulos
**Definición:** Nombre completo de un módulo: `colección.módulo`, por ejemplo `ansible.builtin.copy`. Elimina la duda de qué módulo corre.
**En el curso:** `ansible-lint` tiene la regla `fqcn` para vigilarlo.

---

## G

### gather_facts
**Categoría:** Variables
**Definición:** Opción del play que decide si se recogen facts al empezar. Por omisión es verdadera y cuesta unos segundos por host.
**En el curso:** Se apaga en `prepare.yml` y `verify.yml` de Molecule (sesión 5).

---

### group_vars
**Categoría:** Inventario
**Definición:** Carpeta junto al inventario con archivos nombrados como el grupo, para variables de todo el grupo.
**En el curso:** `inventario/group_vars/vms.yml`.

---

## H

### Handler
**Categoría:** Handlers
**Definición:** Tarea que corre solo si otra la notificó con `notify` porque cambió algo. Corre una vez, al final del play.
**En el curso:** `Recargar ssh` y `Reiniciar fail2ban`.

---

### host_vars
**Categoría:** Inventario
**Definición:** Carpeta junto al inventario con archivos nombrados como el host, para variables de un servidor.
**En el curso:** Diferencias de una VM concreta.

---

## I

### Idempotencia
**Categoría:** Principio
**Definición:** Propiedad de una operación: aplicarla una o muchas veces deja el mismo estado y las corridas siguientes no cambian nada.
**En el curso:** Criterio de aceptación: la segunda corrida reporta `changed=0`.

---

### import_role / include_role
**Categoría:** Roles
**Definición:** Formas de usar un rol desde `tasks`: `import_role` es estático (se procesa al leer el playbook) e `include_role` es dinámico (se procesa al ejecutarse).
**En el curso:** Molecule aplica el rol con `include_role`.

---

### Inventario
**Categoría:** Inventario
**Definición:** Lista de los servidores que Ansible administra, con sus grupos y variables. Formatos principales: INI y YAML.
**En el curso:** `inventario/hosts.yml` con el grupo `vms`.

---

## J

### Jinja2
**Categoría:** Plantillas
**Definición:** Motor de plantillas que usa Ansible para `{{ variables }}` y para archivos `.j2`.
**En el curso:** `PermitRootLogin {{ endurecer_ssh_permit_root_login }}`.

---

## M

### Módulo
**Categoría:** Módulos
**Definición:** Unidad de trabajo de Ansible: un programa pequeño que se copia al servidor, se ejecuta y devuelve JSON con `changed`, `failed` y datos.
**En el curso:** `ansible.builtin.apt`, `ansible.builtin.template`, `community.general.ufw`.

---

## N

### notify
**Categoría:** Handlers
**Definición:** Palabra clave de una tarea que invoca un handler cuando la tarea reporta `changed`.
**En el curso:** La plantilla del drop-in notifica `Recargar ssh`.

---

## P

### Play
**Categoría:** Playbooks
**Definición:** Bloque de un playbook que elige `hosts` y trae tareas o roles.
**En el curso:** `Endurecer las VM` sobre el grupo `vms`.

---

### Playbook
**Categoría:** Playbooks
**Definición:** Archivo YAML con una lista de plays: qué se aplica a qué servidores.
**En el curso:** `site.yml` aplica el rol `endurecer`.

---

### Precedencia de variables
**Categoría:** Variables
**Definición:** Orden que decide qué valor gana cuando una variable está definida en varios lugares. La documentación lista 22 niveles.
**En el curso:** Defaults de rol (2) pierden contra `group_vars`, y todo pierde contra `-e` (22).

---

## R

### raw
**Categoría:** Módulo
**Definición:** Módulo que ejecuta un comando por SSH sin necesitar Python en el servidor. Sirve para instalar Python en imágenes mínimas.
**En el curso:** Lo usa `prepare.yml` de Molecule (sesión 5).

---

### register
**Categoría:** Variables
**Definición:** Palabra clave que guarda el resultado de una tarea en una variable.
**En el curso:** `endurecer_sshd_efectivo` guarda la salida de `sshd -T`.

---

### requirements.yml
**Categoría:** Herramienta
**Definición:** Archivo que lista colecciones y roles con su versión, para instalarlos con `ansible-galaxy`.
**En el curso:** Fija `community.general` 14.0.1.

---

### Rol
**Categoría:** Roles
**Definición:** Carpeta con una estructura fija (`tasks`, `handlers`, `defaults`, `templates`, `meta`) que empaqueta configuración reutilizable.
**En el curso:** `roles/endurecer/`.

---

## S

### sshd -T
**Categoría:** Linux
**Definición:** Opción de `sshd` que imprime la configuración efectiva tras leer todos los archivos. `sshd -t` solo valida la sintaxis.
**En el curso:** Un archivo mal nombrado pasa `-t` pero `-T` muestra `passwordauthentication yes`.

---

## T

### Tag
**Categoría:** Playbooks
**Definición:** Etiqueta de una tarea o rol para correr solo una parte con `--tags`.
**En el curso:** `--tags ssh`, `--tags ufw`.

---

### Task
**Categoría:** Playbooks
**Definición:** Una acción de un play: un módulo con sus argumentos y un `name` legible.
**En el curso:** Cada línea de `PLAY RECAP` cuenta tareas.

---

## U

### UFW
**Categoría:** Seguridad
**Definición:** Interfaz simplificada de firewall en Ubuntu (Uncomplicated Firewall). Política por omisión más reglas por puerto.
**En el curso:** Entrada denegada, SSH permitido antes de activarlo.

---

## V

### validate
**Categoría:** Módulos
**Definición:** Argumento de `template` y `copy` que valida el archivo temporal con un comando antes de instalarlo. `%s` es la ruta temporal.
**En el curso:** `/usr/sbin/sshd -t -f %s` evita instalar un drop-in roto.

---

### venv
**Categoría:** Python
**Definición:** Entorno virtual de Python: carpeta con su propio intérprete y paquetes, aislada del sistema.
**En el curso:** `~/.venvs/ansible` con `ansible-core` fijado.

---

## Y

### YAML
**Categoría:** Formato
**Definición:** Formato de texto con estructura por sangría. Es el lenguaje de inventarios, playbooks y roles.
**En el curso:** `hosts.yml`, `site.yml`, `defaults/main.yml`.

---

