<!-- _class: lead -->

# S04 - Ansible: configurar lo que se creó

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Cada equipo trae su VM Ubuntu 24.04 de Linux para desarrolladores. Antes de empezar: ssh a la VM y python3 --version (debe decir 3.12.x). Esa versión de Python va a decidir qué versión de Ansible instalamos hoy.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, instalar Ansible en la VM | 25 min |
| Parte 1 | Qué resuelve Ansible y cómo trabaja sin agente | 40 min |
| Parte 2 | Inventario, variables y facts | 45 min |
| Parte 3 | Playbooks, módulos, idempotencia y handlers | 55 min |
| Parte 4 | Roles | 35 min |
| Parte 5 | Práctica: un rol que endurece la VM | 70 min |
| Cierre | Errores comunes, tarea y quiz | 30 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. Ya están dentro de los tiempos de arriba. -->

---

## El lema de la sesión

> Un playbook que cambia algo en la segunda corrida no es idempotente.

- Terraform creó el servidor. ¿Quién lo deja configurado?
- Si la respuesta es "yo, a mano, con un checklist", la configuración vive en tu memoria.
- Hoy la pasamos a código **y comprobamos que es honesta**: corrida dos, cero cambios.

<!-- Pregunta al grupo: ¿cuánto tardaron en endurecer su VM a mano en Titulación? ¿Cuánto tardarían en repetirlo en cinco servidores? -->

---

## Al terminar puedes

- Explicar por qué Ansible no necesita un agente y qué necesita el servidor.
- Escribir un inventario en YAML con grupos y variables por grupo y por host.
- Usar facts para que un playbook se adapte al sistema.
- Escribir tareas **idempotentes** con módulos y detectar las que mienten.
- Probar un cambio antes de aplicarlo con `--check --diff`.
- Empaquetar la configuración en un rol y aplicarlo con un comando, las veces que haga falta.

---

## Cómo trabajamos hoy

- Nodo de control: tu VM. Objetivo de la práctica: la misma VM por conexión local (si tu equipo tiene una segunda VM, también sirve por SSH).
- Todo lo que se ve en clase se corre; las salidas de estas láminas son de corridas reales sobre Ubuntu 24.04 (a noviembre de 2026).
- **La regla del curso, otra vez:** si no lo vieron fallar, no lo probaron.

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
lsb_release -d          # Ubuntu 24.04.x LTS
python3 --version       # Python 3.12.x
```

---

<!-- _class: lead -->

# Instalar Ansible
## La trampa de la versión de Python

---

## Dos paquetes con casi el mismo nombre

| Paquete en PyPI | Qué trae |
|---|---|
| `ansible-core` | El motor (`ansible`, `ansible-playbook`, `ansible-galaxy`, `ansible-vault`...) y la colección `ansible.builtin` |
| `ansible` | `ansible-core` más un conjunto grande de colecciones de la comunidad |

- En este curso instalamos `ansible-core` y **pedimos solo las colecciones que usamos**, fijadas por versión.
- Prueba real: con `ansible-core` solo, el módulo de UFW no existe.

```bash
ansible-doc community.general.ufw
# [WARNING]: community.general.ufw was not found
```

Fuente: documentación de instalación de Ansible (docs.ansible.com, "Installing Ansible").

---

## El problema: Python 3.12 en la VM

La documentación publica una tabla de versiones de Python por versión de `ansible-core` (a noviembre de 2026):

| ansible-core | Python del nodo de control | Python de los nodos administrados | Fin de vida |
|---|---|---|---|
| 2.19 | 3.11 a 3.13 | 3.8 a 3.13 | nov 2026 |
| 2.20 | 3.12 a 3.14 | 3.9 a 3.14 | may 2027 |
| 2.21 | 3.12 a 3.14 | 3.9 a 3.14 | nov 2027 |
| 2.22 | (aún no está en la tabla) | (aún no está en la tabla) | - |

- Ubuntu 24.04 trae **Python 3.12.3**. Cabe en 2.20 y 2.21.
- `ansible-core` 2.22.0 está en PyPI desde el 2026-11-02 con `Requires-Python >=3.13`: **no se instala en la VM**.
- Por eso el curso fija **ansible-core 2.21.6** (última 2.21 en PyPI, 2026-11-02).
- Comprobado en un contenedor con Python 3.12.16: `pip install "ansible-core==2.22.0"` termina con `ERROR: No matching distribution found for ansible-core==2.22.0`, y `ansible-core==2.21.6` instala y responde `ansible [core 2.21.6]`.

Fuente: "Release and maintenance" de la documentación de Ansible y la ficha de `ansible-core` en PyPI.

<!-- Esta es una trampa real: quien hace pip install ansible-core sin versión en una máquina con 3.12 se queda con otra versión sin enterarse, o falla. Fijar la versión es parte de la infraestructura como código. -->

---

## Instalación recomendada en la VM

```bash
sudo apt-get update
sudo apt-get install -y python3-venv
python3 -m venv ~/.venvs/ansible
~/.venvs/ansible/bin/pip install "ansible-core==2.21.6"
echo 'export PATH="$HOME/.venvs/ansible/bin:$PATH"' >> ~/.bashrc
```

Comprobación:

```bash
ansible --version | head -2
# ansible [core 2.21.6]
```

- La documentación presenta `pip` como la vía estándar y `pipx` como alternativa cuando no se puede usar `pip`.
- Un entorno virtual por proyecto evita que una actualización del sistema cambie tu Ansible.
- Las colecciones se instalan por proyecto con un `requirements.yml` (lo vemos en la práctica).

Fuente: "Installing Ansible" (docs.ansible.com).

---

<!-- _class: lead -->

# Parte 1
## Qué resuelve Ansible y cómo trabaja

---

## El problema: el servidor que nadie puede repetir

- Terraform entrega una máquina con un sistema operativo limpio.
- Alguien entra por SSH, instala paquetes, edita archivos, abre puertos.
- Tres meses después: nadie sabe qué hizo, el segundo servidor sale distinto, y el checklist está desactualizado.

Lo que buscamos: **describir el estado final** de la configuración y que una herramienta lo alcance las veces que haga falta.

> Terraform dice **qué recursos existen**. Ansible dice **cómo quedan configurados por dentro**.

Fuente: Meijer et al., *Ansible: Up and Running* (2022), cap. 1.

---

## Sin agente: SSH y Python

```text
  nodo de control                         servidor administrado
  +-----------------+     SSH            +------------------------+
  | ansible-playbook| -----------------> | copia un módulo (.py)  |
  | inventario      |                    | lo ejecuta con python3 |
  | playbooks/roles | <----------------- | devuelve JSON          |
  +-----------------+     resultado      +------------------------+
```

- No hay demonio que instalar ni puerto nuevo que abrir: solo **SSH y Python** en el servidor.
- Ansible empuja el trabajo (modelo *push*) cuando tú lo corres.
- El servidor necesita Python (versiones 3.9 a 3.14 para ansible-core 2.20 y 2.21, según la tabla de soporte).
- Cada tarea es un **módulo** que se copia, se ejecuta y reporta si **cambió** algo.

Fuente: "Release and maintenance" y guía de usuario de Ansible, sección de conexión.

---

## Las piezas

| Pieza | Qué es | Archivo típico |
|---|---|---|
| Inventario | Qué servidores y en qué grupos | `inventario/hosts.yml` |
| Playbook | Qué se aplica a qué grupo | `site.yml` |
| Tarea | Una acción, con un módulo | dentro del playbook o del rol |
| Módulo | El código que hace el trabajo | `ansible.builtin.apt`, `ansible.builtin.template`... |
| Rol | Un paquete reutilizable de tareas, plantillas y variables | `roles/endurecer/` |
| Colección | Módulos y roles distribuidos como paquete | `community.general` |

- El nombre completo de un módulo (**FQCN**) es `colección.módulo`: `ansible.builtin.copy`, `community.general.ufw`.
- Escribe siempre el FQCN: no hay duda de qué módulo corre, y `ansible-lint` tiene la regla `fqcn` ("Use FQCN for builtin actions") que lo vigila.

---

## Demostración: el primer comando

Inventario mínimo, guardado como `inventario/hosts.yml`:

```yaml
---
all:
  children:
    vms:
      hosts:
        vm-lab:
          ansible_connection: local
```

```bash
ansible vms -i inventario/hosts.yml -m ansible.builtin.ping
# vm-lab | SUCCESS => {
#     "changed": false,
#     "ping": "pong"
# }
```

- `ping` de Ansible **no es ICMP**: comprueba que puede conectarse y ejecutar un módulo de Python.
- Si ves `pong`, tienes conexión, Python y permisos suficientes.

---

## Error típico: "UNREACHABLE" o "no Python"

- `UNREACHABLE`: SSH falló (usuario, llave, puerto, firewall). **Se arregla con SSH, no con Ansible.** Prueba primero `ssh <usuario>@<host>`.
- `/usr/bin/python3: not found`: el servidor administrado no trae Python. En imágenes mínimas hay que instalarlo con el módulo `raw`, que no necesita Python.
- `Permission denied` al instalar paquetes: falta `become: true` o el usuario no tiene sudo.

> Regla: si `ansible -m ping` no responde `pong`, no sigas. Un playbook no arregla una conexión rota.

---

<!-- _class: lead -->

# Parte 2
## Inventario, variables y facts

---

## El inventario: qué servidores existen

Dos formatos principales: INI y YAML. Usamos YAML.

```yaml {1-3|4-12|*}
---
all:
  children:
    vms:
      hosts:
        vm-lab:
          ansible_host: 127.0.0.1
          ansible_port: 2222
          ansible_user: lab
          ansible_ssh_private_key_file: ~/.ssh/id_ed25519
        vm-lab2:
          ansible_host: 192.0.2.20
```

- `all` y `ungrouped` existen siempre. Un host puede estar en varios grupos; un grupo puede contener otros con `children`.
- Variables de conexión: `ansible_host`, `ansible_port`, `ansible_user`, `ansible_ssh_private_key_file`.
- `192.0.2.20` es una dirección de documentación (RFC 5737): **Ejemplo ilustrativo**.

Fuente: "How to build your inventory" (docs.ansible.com).

---

## Ver el inventario antes de usarlo

```bash
ansible-inventory -i inventario/hosts.yml --graph
# @all:
#   |--@ungrouped:
#   |--@vms:
#   |  |--vm-lab
```

```bash
ansible-inventory -i inventario/hosts.yml --host vm-lab
```

- `--graph` te enseña la jerarquía; `--host` te enseña **todas las variables** que Ansible resolvió para ese host.
- Antes de culpar al playbook, mira qué variables ve realmente el host.

---

## group_vars y host_vars

Las variables que no son de conexión van en archivos junto al inventario:

```text
inventario/
├── hosts.yml
├── group_vars/
│   ├── all.yml          # para todos
│   └── vms.yml          # para el grupo vms
└── host_vars/
    └── vm-lab.yml       # para un host
```

```yaml
# inventario/group_vars/vms.yml
---
endurecer_ufw_puertos_tcp: [22]
endurecer_fail2ban_maxretry: 5
```

- El archivo se llama como el grupo o el host. Extensiones válidas: `.yml`, `.yaml`, `.json` o ninguna.
- También pueden ser **directorios** con varios archivos dentro.
- Datos que difieren entre entornos (dev, staging) van aquí, no dentro del rol.

Fuente: "How to build your inventory", sección de variables.

---

## Precedencia: quién gana cuando hay dos valores

La documentación lista 22 niveles, del más débil al más fuerte. Los que importan hoy:

| Nivel | Origen |
|---|---|
| 2 | Role defaults (`defaults/main.yml`) |
| 4 y 6 | `group_vars/all` y `group_vars/*` del inventario |
| 9 | `host_vars/*` del inventario |
| 11 | Host facts |
| 12 | Play vars |
| 15 | Role vars (`vars/main.yml`) |
| 19 | Variables registradas con `register` y `set_fact` |
| 22 | Extra vars (`-e`): ganan siempre |

- **Defaults de rol = lo que el usuario del rol puede cambiar.** Role vars = lo que el rol impone.
- Regla práctica: pon en `defaults/` todo lo configurable; usa `vars/` casi nunca.
- Si dudas de quién ganó, corre con `-e` para forzar o usa `ansible-inventory --host`.

Fuente: "Using variables", sección de precedencia (docs.ansible.com).

---

## Facts: lo que Ansible descubre del servidor

Al empezar un play, Ansible corre el módulo `setup` (`gather_facts: true` por omisión) y guarda lo que encuentra:

```bash
ansible vms -i inventario/hosts.yml -m ansible.builtin.setup -a 'filter=ansible_distribution*'
```

```text
"ansible_distribution": "Ubuntu",
"ansible_distribution_major_version": "24",
"ansible_distribution_release": "noble",
"ansible_distribution_version": "24.04",
```

- Se leen en un playbook como `ansible_facts['distribution']` o `ansible_distribution_release`.
- Sirven para decidir: `when: ansible_facts['distribution'] == 'Ubuntu'`.
- Recoger facts cuesta unos segundos por host: `gather_facts: false` cuando no los usas.

Fuente: "Discovering variables: facts and magic variables" (docs.ansible.com); salida de una corrida real en Ubuntu 24.04.

---

## Error típico: una variable que no existe

```yaml
- name: Usar una variable que no existe
  ansible.builtin.debug:
    msg: "{{ puerto_ssh }}"
```

```text
Error while resolving value for 'msg': 'puerto_ssh' is undefined
```

- Ansible falla en voz alta: **una variable indefinida es error**, no cadena vacía.
- Un typo en el nombre (`endurecer_ufw_puerto_tcp` frente a `..._puertos_tcp`) se ve así.
- Ponle **prefijo con el nombre del rol** a sus variables (`endurecer_...`): evita choques entre roles y se ve de dónde viene cada una.

---

<!-- _class: lead -->

# Parte 3
## Playbooks, módulos, idempotencia y handlers

---

## Un playbook

```yaml {1-4|5-12|*}
---
- name: Preparar el directorio de datos
  hosts: vms
  become: true
  tasks:
    - name: Crear la carpeta
      ansible.builtin.file:
        path: /srv/datos
        state: directory
        mode: "0755"

    - name: Escribir el estado
      ansible.builtin.copy:
        content: "configurado\n"
        dest: /srv/datos/estado.txt
        mode: "0644"
```

- Un playbook es una lista de **plays**; cada play elige `hosts` y trae `tasks`.
- `become: true` sube privilegios (sudo) para el play.
- Cada tarea lleva `name`: es lo que verás en la salida y en los logs. Escribe nombres que digan **qué queda**, no qué hace el comando.

---

## Idempotencia: la propiedad que importa

> Idempotente: aplicarlo una vez o diez veces deja el sistema en el mismo estado, y las corridas después de la primera **no cambian nada**.

Los módulos de Ansible declaran **estado deseado**, no pasos:

| Declarativo (módulo) | Imperativo (comando) |
|---|---|
| `file: state=directory` | `command: mkdir -p ...` |
| `copy: content=... dest=...` | `shell: echo ... > archivo` |
| `apt: name=ufw state=present` | `command: apt-get install ufw` |

- El módulo mira primero cómo está el servidor. Si ya está bien, reporta `ok` y no toca nada.
- `command` y `shell` **no saben** si cambiaron algo: siempre reportan `changed`.

Fuente: Meijer et al., *Ansible: Up and Running* (2022), cap. 3.

---

## Verlo fallar: la segunda corrida

Playbook con `command` y `shell`:

```yaml
- name: Crear carpeta de datos
  ansible.builtin.command: mkdir -p /srv/datos

- name: Anotar la fecha de configuracion
  ansible.builtin.shell: echo "configurado" > /srv/datos/estado.txt
```

Primera corrida y segunda corrida (salida real, recortada):

```text
PLAY RECAP *********************************
vm-lab : ok=3    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0

PLAY RECAP *********************************
vm-lab : ok=3    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

- La segunda corrida **sigue diciendo `changed=2`** aunque no hay nada que cambiar. El resumen miente: ya no sirve como alarma.
- `ok=3` incluye la tarea invisible de recoger facts.

---

## El arreglo: módulos, o decirle a Ansible cuándo cambió

Mejor: el módulo correcto.

```yaml
- name: Crear carpeta de datos
  ansible.builtin.file:
    path: /srv/datos
    state: directory
    mode: "0755"
```

Salida real: primera corrida `changed=2`, segunda `changed=0`.

Si de verdad necesitas `command`, declara cuándo cambia:

```yaml
- name: Inicializar la base una sola vez
  ansible.builtin.command: /opt/app/inicializar.sh
  args:
    creates: /opt/app/.inicializada

- name: Leer el estado (nunca cambia nada)
  ansible.builtin.command: /usr/sbin/sshd -T
  changed_when: false
```

- `creates:` salta la tarea si el archivo ya existe. `changed_when:` decide qué cuenta como cambio.
- Fragmento: `/opt/app/inicializar.sh` es un ejemplo, no existe en tu VM.

---

## Check y diff: probar antes de aplicar

```bash
ansible-playbook idempotente.yml --check --diff
```

```text
TASK [Anotar el estado] ***************************
--- before: /srv/datos/estado.txt
+++ after: /srv/datos/estado.txt
@@ -1 +1 @@
-configurado
+otro

changed: [vm-lab]
```

- `--check` simula sin cambiar nada; `--diff` muestra qué líneas cambiarían en archivos y plantillas.
- Los módulos que no soportan check mode no hacen nada ni reportan nada. `command` y `shell` no son confiables en check: **no es una prueba completa**.
- `--diff` puede mostrar secretos: en tareas sensibles usa `diff: false`.

Fuente: "Validating tasks: check mode and diff mode" (docs.ansible.com).

---

## Handlers: reaccionar solo si algo cambió

```yaml
- name: Escribir el drop-in de SSH
  ansible.builtin.template:
    src: 10-endurecer.conf.j2
    dest: /etc/ssh/sshd_config.d/10-endurecer.conf
    validate: /usr/sbin/sshd -t -f %s
  notify: Recargar ssh

# handlers/main.yml
- name: Recargar ssh
  ansible.builtin.service:
    name: ssh
    state: reloaded
```

- `notify` llama al handler **solo si la tarea reportó `changed`**. Segunda corrida: no cambia, no se recarga el servicio.
- Los handlers corren **al final del play**, una sola vez aunque varias tareas los llamen. `meta: flush_handlers` los adelanta.
- `validate:` corre el comando con el archivo temporal **antes** de instalarlo: si falla, el archivo bueno no se toca.

---

## Become, tags y orden

- `become: true` en el play o en la tarea. Para sudo sin contraseña, el usuario necesita `NOPASSWD` o se pasa con `--ask-become-pass`.
- `tags` etiquetan tareas para correr un pedazo: `ansible-playbook site.yml --tags ssh`.
- Las tareas corren en el orden escrito, de arriba abajo, en cada host.
- `--limit vm-lab` restringe a un host; `--list-tasks` lista lo que correría sin ejecutar.

Fuente: página del comando `ansible-playbook` (docs.ansible.com, CLI).

---

<!-- _class: lead -->

# Parte 4
## Roles

---

## El problema: un playbook de 400 líneas

- El playbook crece, se copia a otro proyecto, y los dos divergen.
- No puedes probar la parte de SSH sin correr toda la configuración.
- Un **rol** es una carpeta con convenciones: Ansible sabe dónde buscar cada cosa.

```bash
ansible-galaxy role init demo-rol
```

```text
demo-rol/defaults/main.yml     demo-rol/handlers/main.yml
demo-rol/meta/main.yml         demo-rol/README.md
demo-rol/tasks/main.yml        demo-rol/tests/inventory
demo-rol/tests/test.yml        demo-rol/vars/main.yml
```

Fuente: salida real de `ansible-galaxy role init` con ansible-core 2.21.6.

---

## Qué va en cada carpeta

| Carpeta | Contenido (según la documentación) |
|---|---|
| `tasks/main.yml` | Lista de tareas que el rol ofrece |
| `handlers/main.yml` | Handlers que el rol aporta al play |
| `defaults/main.yml` | Valores de **muy baja precedencia**: lo que se puede cambiar |
| `vars/main.yml` | Variables de **alta precedencia** del rol |
| `templates/` | Plantillas Jinja2 (`.j2`) |
| `files/` | Archivos que se copian tal cual |
| `meta/main.yml` | Metadatos y dependencias del rol |

- Los módulos propios o los plugins van en `library/`, `module_utils/`, etc.
- Un rol no es un playbook: **no dice a qué hosts se aplica**. Eso lo decide quien lo usa.

Fuente: "Roles" (docs.ansible.com, playbook guide).

---

## Usar un rol

```yaml
---
- name: Endurecer las VM
  hosts: vms
  become: true
  roles:
    - role: endurecer
      vars:
        endurecer_fail2ban_maxretry: 3
```

- `roles:` es la forma clásica y **estática**: los roles corren antes que las `tasks` del play.
- `import_role` (estático) e `include_role` (dinámico) lo hacen desde `tasks:`. Con `include_role`, las etiquetas aplican solo a la inclusión.
- Ansible busca los roles en `roles_path` y en una carpeta `roles/` junto al playbook.

Fuente: "Roles" (docs.ansible.com): "Using roles" y "Role search path".

---

## Un rol bien hecho

- **Una responsabilidad**: `endurecer` endurece; no instala tu aplicación.
- Variables con prefijo del rol y valores por omisión razonables en `defaults/`.
- Tareas partidas por tema (`ssh.yml`, `ufw.yml`, `fail2ban.yml`) con `import_tasks` y etiquetas.
- Plantillas con encabezado "Gestionado por Ansible, no editar a mano".
- Verificaciones **dentro** del rol: una `assert` que compruebe el estado efectivo.
- Un `README.md` con variables, ejemplo de uso y qué hace.

---

<!-- _class: lead -->

# Parte 5
## Práctica: un rol que endurece la VM

---

## Lo que vamos a construir

El checklist de endurecimiento de Titulación y de la sesión 5 de Linux, como rol:

| Control | Cómo queda en el rol |
|---|---|
| SSH solo con llave | Drop-in `10-endurecer.conf`: `PasswordAuthentication no` y `PermitRootLogin no` |
| El drop-in realmente manda | Comprobación con `sshd -T` (configuración **efectiva**) |
| Firewall | UFW: entrada denegada, SSH permitido **antes** de activarlo |
| Fuerza bruta | fail2ban con jaula `sshd` |

- Cada pieza, con su "verlo fallar" en la guía de práctica.
- Entrega: el rol, la salida de la segunda corrida con `changed=0` y la evidencia de cada fallo.

---

## El orden de los .conf importa

OpenSSH usa **el primer valor obtenido** de cada opción. Ubuntu incluye los archivos de `sshd_config.d` en orden léxico:

```text
/etc/ssh/sshd_config.d/50-cloud-init.conf   PasswordAuthentication yes
/etc/ssh/sshd_config.d/99-endurecer.conf    PasswordAuthentication no
```

```bash
sshd -t && echo "sintaxis OK"    # sintaxis OK
sshd -T | grep passwordauth      # passwordauthentication yes
```

- **`sshd -t` dice que está bien y `sshd -T` dice que la contraseña sigue entrando.** Gana el `50`.
- Por eso el rol usa el prefijo `10-` y comprueba el resultado efectivo con `sshd -T`.

Fuentes: página de manual `sshd_config(5)` ("the first obtained value will be used"); `Include /etc/ssh/sshd_config.d/*.conf` en la línea 12 del `sshd_config` de la imagen de Ubuntu 24.04 con la que se probó.

---

## Firewall: permitir SSH antes de activar

```yaml {1-6|8-12|*}
- name: Permitir los puertos TCP ANTES de activar el firewall
  community.general.ufw:
    rule: allow
    port: "{{ item | string }}"
    proto: tcp
  loop: "{{ endurecer_ufw_puertos_tcp }}"

- name: Politica por omision y activar
  community.general.ufw:
    state: enabled
    policy: deny
    direction: incoming
```

- Si activas UFW con `deny incoming` sin permitir SSH, te quedas fuera de tu propia máquina.
- El orden de las tareas **es** la protección. Y lo confirma Molecule la próxima sesión.
- `community.general.ufw` no viene en `ansible-core`: va en `requirements.yml`.

Fuente: documentación del módulo `community.general.ufw` (versión 14.0.1, comprobada instalando la colección).

---

## Primera corrida y segunda corrida

Primera corrida contra un Ubuntu 24.04 limpio (resumen real):

```text
PLAY RECAP *********************************
vm-lab : ok=13   changed=9    unreachable=0    failed=0
```

Segunda corrida sobre lo mismo:

```text
PLAY RECAP *********************************
vm-lab : ok=11   changed=0    unreachable=0    failed=0
```

Estado resultante comprobado por fuera del playbook:

```text
passwordauthentication no
permitrootlogin no
Status: active
Default: deny (incoming), allow (outgoing), deny (routed)
22/tcp                     ALLOW IN    Anywhere
```

- Dos corridas, **cero cambios en la segunda**: ese es el criterio de aceptación.
- La diferencia de `ok` (13 y 11) son los dos handlers (recargar ssh, reiniciar fail2ban): solo corren cuando algo cambió.

---

## Verlo fallar (1): la assert que atrapa al archivo equivocado

```bash
ansible-playbook site.yml --tags ssh -e endurecer_ssh_dropin=99-endurecer.conf
```

```text
TASK [endurecer : Comprobar que ningun otro .conf pisa el endurecimiento]
[ERROR]: Task failed: Action failed: sshd -T no muestra el valor endurecido:
otro archivo de sshd_config.d lo esta ganando.
fatal: [vm-lab]: FAILED! => {"assertion": "'passwordauthentication no' in endurecer_sshd_efectivo.stdout_lines", ...}
```

- El rol falla **antes** de dar el servidor por endurecido.
- Quitar el archivo `99-` y volver a correr sin `-e` regresa a `failed=0`.

---

## Verlo fallar (2): el playbook que miente

Cuando el equipo agrega un `command` sin `changed_when`:

- La segunda corrida deja de decir `changed=0`.
- Nadie nota cuando algo cambia de verdad, porque siempre cambia.
- `ansible-lint` lo marca con la regla `no-changed-when` (la sesión 5 lo trae al CI).

```text
no-changed-when: Commands should not change things if nothing needs doing.
no-idempotente.yml:6 Task/Handler: Crear carpeta de datos
```

---

## Errores comunes de la sesión

| Síntoma | Causa | Qué hacer |
|---|---|---|
| `pip install ansible-core` instala algo distinto a lo esperado | Python 3.12 y versión sin fijar | Fijar `==2.21.6` en un venv |
| `community.general.ufw was not found` | Falta la colección | `ansible-galaxy collection install -r requirements.yml` |
| `changed` en cada corrida | `command`/`shell` sin `changed_when` o `creates` | Usar un módulo o declarar el cambio |
| La contraseña SSH sigue entrando | Otro `.conf` en orden léxico anterior | `sshd -T` y renombrar con prefijo `10-` |
| Te quedas fuera por SSH | Firewall activo sin regla de SSH | Permitir antes de activar; ventana con la VM abierta |
| `is undefined` | Nombre de variable mal escrito | `ansible-inventory --host` y prefijo del rol |

---

## Lo que NO hace Ansible por sí solo

- No arregla un SSH roto ni una VM apagada.
- No revierte: una tarea que cambió algo **no se deshace** si la siguiente falla. Se corrige hacia adelante.
- No prueba tu rol en un servidor limpio: eso lo hace Molecule (sesión 5).
- No guarda secretos a salvo: para eso `ansible-vault` y gestores de secretos (sesión 5).

---

## Referencias (a noviembre de 2026)

- Ansible, instalación: https://docs.ansible.com/projects/ansible/latest/installation_guide/intro_installation.html
- Ansible, versiones y soporte (tabla de Python por ansible-core): https://docs.ansible.com/projects/ansible/latest/reference_appendices/release_and_maintenance.html
- Ansible, inventario: https://docs.ansible.com/projects/ansible/latest/inventory_guide/intro_inventory.html
- Ansible, variables y precedencia: https://docs.ansible.com/projects/ansible/latest/playbook_guide/playbooks_variables.html
- Ansible, roles: https://docs.ansible.com/projects/ansible/latest/playbook_guide/playbooks_reuse_roles.html
- Ansible, modo check y diff: https://docs.ansible.com/projects/ansible/latest/playbook_guide/playbooks_checkmode.html
- OpenSSH, `sshd_config(5)`: https://man.openbsd.org/sshd_config
- ansible-core 2.21.6 y 2.22.0 en PyPI: https://pypi.org/project/ansible-core/
- Versiones usadas en esta sesión: ansible-core 2.21.6, community.general 14.0.1, Ubuntu 24.04 con Python 3.12.3.

---

## Tarea y siguiente sesión

**Tarea:** el checklist completo de la VM de Titulación, hecho rol de Ansible (ver `tarea.md`).

### S05 - Calidad y CI para Ansible

- Depurar un playbook con `-v`, `debug`, `--start-at-task` y `--step`.
- `ansible-lint` y sus perfiles.
- Molecule: probar el rol en un contenedor limpio, con idempotencia y verificación.
- Un workflow de GitHub Actions que lo corre en cada pull request.

> Un rol sin prueba se rompe la primera vez que alguien más lo usa.

---

## Quiz: Ansible, configurar lo que se creó

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en esta clase y en la nota `09-cicd/06-ansible` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
