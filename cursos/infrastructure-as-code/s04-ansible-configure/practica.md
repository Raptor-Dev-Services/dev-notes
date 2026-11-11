# Práctica - S04 - Ansible: configurar lo que se creó

**Infraestructura como código**
**Duración:** 70 minutos, por equipo, en su VM.

---

## Objetivo

Escribir un **rol de Ansible** que deja la VM con los controles de endurecimiento de Titulación y de la sesión 5 de Linux (SSH solo con llave, firewall y fail2ban), correrlo dos veces y demostrar que la segunda corrida reporta **cero cambios**. Además, ver fallar tres cosas a propósito:

1. Un playbook con `command` y `shell` que cambia "algo" en cada corrida.
2. Un archivo de SSH con el nombre equivocado que **no** endurece nada, aunque `sshd -t` diga que está bien.
3. Una variable mal escrita.

Las salidas de esta guía son de corridas reales con `ansible-core` 2.21.6 sobre Ubuntu 24.04 (a noviembre de 2026). Tu salida puede variar en tiempos y en el orden de los mensajes; los números de `changed` deben coincidir.

---

## Antes de empezar

```bash
ssh <USUARIO>@<IP_DE_SU_VM>
lsb_release -d          # Ubuntu 24.04.x LTS
python3 --version       # Python 3.12.x
sudo -v                 # debes tener sudo
```

**Muy importante antes de tocar SSH y el firewall:**

- Verifica que entras **con llave** (sesión 5 de Linux). Si todavía entras con contraseña, el rol te dejará fuera.
- Deja **una segunda terminal con sesión abierta** en la VM mientras corres el rol. Si algo sale mal, desde ahí lo corriges.
- Si tu equipo tiene consola de la VM (acceso fuera de SSH), ubica cómo abrirla.

---

## Paso 1 - Instalar Ansible fijado

```bash
sudo apt-get update
sudo apt-get install -y python3-venv
python3 -m venv ~/.venvs/ansible
~/.venvs/ansible/bin/pip install "ansible-core==2.21.6"
export PATH="$HOME/.venvs/ansible/bin:$PATH"
ansible --version | head -2
```

Salida esperada:

```
ansible [core 2.21.6]
  config file = None
```

> ¿Por qué 2.21.6 y no la última? `ansible-core` 2.22.0 pide Python 3.13 o más y tu VM trae 3.12. Comprobado en un contenedor con Python 3.12: `pip install "ansible-core==2.22.0"` termina con `ERROR: No matching distribution found for ansible-core==2.22.0`.

---

## Paso 2 - El proyecto

```bash
mkdir -p ~/iac-lab/ansible/inventario/group_vars && cd ~/iac-lab/ansible
git init -q
```

`ansible.cfg`:

```ini
[defaults]
inventory = inventario/hosts.yml
roles_path = roles
collections_path = ./colecciones
host_key_checking = False
interpreter_python = auto_silent

[ssh_connection]
pipelining = True
```

`requirements.yml` (las colecciones se fijan, como las dependencias de cualquier proyecto):

```yaml
---
collections:
  - name: community.general
    version: "14.0.1"
```

`inventario/hosts.yml` (la VM se administra a sí misma por conexión local):

```yaml
---
all:
  children:
    vms:
      hosts:
        vm-lab:
          ansible_connection: local
```

> Si tu equipo tiene una **segunda VM**, cámbiala por una conexión SSH: `ansible_host: <IP_DE_LA_SEGUNDA_VM>`, `ansible_user: <USUARIO>` y `ansible_ssh_private_key_file: ~/.ssh/id_ed25519`. Los pasos siguientes son idénticos.

```bash
ansible-galaxy collection install -r requirements.yml -p colecciones
ansible vms -m ansible.builtin.ping
```

Salida esperada del `ping`:

```
vm-lab | SUCCESS => {
    "changed": false,
    "ping": "pong"
}
```

Agrega al `.gitignore` la carpeta `colecciones/`:

```bash
echo "colecciones/" > .gitignore
```

---

## Paso 3 - Verlo fallar: el playbook que miente

Crea `no-idempotente.yml`:

```yaml
---
- name: Playbook que miente
  hosts: vms
  become: true
  tasks:
    - name: Crear carpeta de datos
      ansible.builtin.command: mkdir -p /srv/datos

    - name: Anotar la fecha de configuracion
      ansible.builtin.shell: echo "configurado" > /srv/datos/estado.txt
```

Córrelo **dos veces**:

```bash
ansible-playbook no-idempotente.yml -K
ansible-playbook no-idempotente.yml -K
```

(`-K` pide tu contraseña de sudo.) Salida real, resumen de cada corrida:

```
PLAY RECAP *********************************************************************
vm-lab : ok=3    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0

PLAY RECAP *********************************************************************
vm-lab : ok=3    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

**Lo que debes ver:** la segunda corrida **sigue diciendo `changed=2`**. Anótalo en tu evidencia: este playbook no es idempotente.

Ahora arréglalo con módulos. Crea `idempotente.yml`:

```yaml
---
- name: Playbook honesto
  hosts: vms
  become: true
  tasks:
    - name: Crear carpeta de datos
      ansible.builtin.file:
        path: /srv/datos
        state: directory
        mode: "0755"

    - name: Anotar el estado
      ansible.builtin.copy:
        content: "configurado\n"
        dest: /srv/datos/estado.txt
        mode: "0644"
```

Antes de correrlo, borra lo que dejó el mentiroso para partir de cero: `sudo rm -r /srv/datos`. Córrelo dos veces:

```
PLAY RECAP ***
vm-lab : ok=3    changed=2    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0

PLAY RECAP ***
vm-lab : ok=3    changed=0    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

`changed=0` en la segunda: así se ve la idempotencia. Prueba también el modo de simulación. Cambia el contenido del archivo a `otro` en el playbook y corre:

```bash
ansible-playbook idempotente.yml -K --check --diff
```

```
TASK [Anotar el estado] ********************************************************
--- before: /srv/datos/estado.txt
+++ after: /srv/datos/estado.txt
@@ -1 +1 @@
-configurado
+otro

changed: [vm-lab]
```

`--check` no tocó el archivo. Vuelve a poner `configurado` y limpia: `sudo rm -r /srv/datos`.

---

## Paso 4 - El esqueleto del rol

```bash
ansible-galaxy role init roles/endurecer
ls roles/endurecer
```

Salida esperada:

```
defaults  handlers  meta  README.md  tasks  tests  vars
```

Borra lo que no vamos a usar y crea `templates/`:

```bash
rm -r roles/endurecer/tests roles/endurecer/vars
mkdir roles/endurecer/templates
```

---

## Paso 5 - Variables con valores por omisión

`roles/endurecer/defaults/main.yml`:

```yaml
---
# Valores que quien usa el rol puede cambiar (precedencia baja a proposito).
endurecer_ssh_dropin: 10-endurecer.conf
endurecer_ssh_permit_root_login: "no"
endurecer_ssh_max_auth_tries: 3
endurecer_ufw_puertos_tcp: [22]
endurecer_fail2ban_maxretry: 5
endurecer_fail2ban_findtime: 10m
endurecer_fail2ban_bantime: 1h
```

`roles/endurecer/meta/main.yml`:

```yaml
---
galaxy_info:
  role_name: endurecer
  namespace: raptor_academy
  author: Raptor Dev Services Academy
  description: Endurece una VM Ubuntu 24.04 (SSH con llave, UFW, fail2ban).
  license: MIT
  min_ansible_version: "2.20"
  platforms:
    - name: Ubuntu
      versions:
        - noble
dependencies: []
```

---

## Paso 6 - SSH solo con llave

`roles/endurecer/templates/10-endurecer.conf.j2`:

```
# Gestionado por Ansible (rol endurecer). No editar a mano.
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin {{ endurecer_ssh_permit_root_login }}
MaxAuthTries {{ endurecer_ssh_max_auth_tries }}
```

`roles/endurecer/tasks/ssh.yml`:

```yaml
---
- name: Instalar openssh-server
  ansible.builtin.apt:
    name: openssh-server
    state: present
    update_cache: true
    cache_valid_time: 3600

- name: Escribir el drop-in (el primero en orden lexico gana)
  ansible.builtin.template:
    src: 10-endurecer.conf.j2
    dest: "/etc/ssh/sshd_config.d/{{ endurecer_ssh_dropin }}"
    owner: root
    group: root
    mode: "0644"
    validate: /usr/sbin/sshd -t -f %s
  notify: Recargar ssh

- name: Aplicar ahora los cambios pendientes de ssh
  ansible.builtin.meta: flush_handlers

- name: Leer la configuracion EFECTIVA de sshd
  ansible.builtin.command: /usr/sbin/sshd -T
  register: endurecer_sshd_efectivo
  changed_when: false

- name: Comprobar que ningun otro .conf pisa el endurecimiento
  ansible.builtin.assert:
    that:
      - "'passwordauthentication no' in endurecer_sshd_efectivo.stdout_lines"
      - "'permitrootlogin ' ~ endurecer_ssh_permit_root_login in endurecer_sshd_efectivo.stdout_lines"
    fail_msg: "sshd -T no muestra el valor endurecido: otro archivo de sshd_config.d lo esta ganando."
    quiet: true
```

`roles/endurecer/handlers/main.yml`:

```yaml
---
- name: Recargar ssh
  ansible.builtin.service:
    name: ssh
    state: reloaded

- name: Reiniciar fail2ban
  ansible.builtin.service:
    name: fail2ban
    state: restarted
```

> `changed_when: false` en la tarea que lee: **leer no es cambiar**. Sin eso, la segunda corrida reportaría un cambio falso.

---

## Paso 7 - Firewall y fail2ban

`roles/endurecer/tasks/ufw.yml`:

```yaml
---
- name: Instalar ufw
  ansible.builtin.apt:
    name: ufw
    state: present
    update_cache: true
    cache_valid_time: 3600

- name: Permitir los puertos TCP ANTES de activar el firewall
  community.general.ufw:
    rule: allow
    port: "{{ item | string }}"
    proto: tcp
  loop: "{{ endurecer_ufw_puertos_tcp }}"

- name: Politica por omision (entrada denegada, salida permitida) y activar
  community.general.ufw:
    state: enabled
    policy: deny
    direction: incoming
```

`roles/endurecer/templates/sshd.local.j2`:

```
# Gestionado por Ansible (rol endurecer). No editar a mano.
[sshd]
enabled  = true
maxretry = {{ endurecer_fail2ban_maxretry }}
findtime = {{ endurecer_fail2ban_findtime }}
bantime  = {{ endurecer_fail2ban_bantime }}
```

`roles/endurecer/tasks/fail2ban.yml`:

```yaml
---
- name: Instalar fail2ban
  ansible.builtin.apt:
    name: fail2ban
    state: present
    update_cache: true
    cache_valid_time: 3600

- name: Jaula sshd
  ansible.builtin.template:
    src: sshd.local.j2
    dest: /etc/fail2ban/jail.d/10-endurecer.local
    owner: root
    group: root
    mode: "0644"
  notify: Reiniciar fail2ban

- name: Fail2ban arrancado y habilitado
  ansible.builtin.service:
    name: fail2ban
    state: started
    enabled: true
```

`roles/endurecer/tasks/main.yml`:

```yaml
---
- name: SSH solo con llave
  ansible.builtin.import_tasks: ssh.yml
  tags: [ssh]

- name: Firewall UFW
  ansible.builtin.import_tasks: ufw.yml
  tags: [ufw]

- name: Fail2ban
  ansible.builtin.import_tasks: fail2ban.yml
  tags: [fail2ban]
```

---

## Paso 8 - El playbook y la primera corrida

`site.yml`:

```yaml
---
- name: Endurecer las VM
  hosts: vms
  become: true
  roles:
    - role: endurecer
```

```bash
ansible-playbook site.yml --syntax-check
ansible-playbook site.yml -K
```

Resumen esperado de la **primera** corrida:

```
PLAY RECAP *********************************************************************
vm-lab : ok=13   changed=9    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

(Si tu VM ya tenía alguno de los paquetes o el archivo del drop-in, `changed` será menor: lo importante es lo que sigue.)

**La segunda corrida:**

```bash
ansible-playbook site.yml -K
```

```
PLAY RECAP *********************************************************************
vm-lab : ok=11   changed=0    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0
```

`changed=0`. Si en tu segunda corrida aparece un `changed`, **encuentra cuál tarea fue** (la salida marca `changed:` en esa tarea) y corrígela antes de seguir.

---

## Paso 9 - Comprobar el resultado por fuera de Ansible

```bash
sudo sshd -T | grep -E '^(passwordauthentication|permitrootlogin|maxauthtries|kbdinteractiveauthentication) '
sudo ufw status verbose
sudo fail2ban-client status
```

Salida esperada:

```
maxauthtries 3
permitrootlogin no
passwordauthentication no
kbdinteractiveauthentication no
```

```
Status: active
Default: deny (incoming), allow (outgoing), deny (routed)

To                         Action      From
22/tcp                     ALLOW IN    Anywhere
22/tcp (v6)                ALLOW IN    Anywhere (v6)
```

```
Status
|- Number of jail:	1
`- Jail list:	sshd
```

Desde tu laptop, en una terminal nueva, comprueba que la contraseña ya no entra:

```bash
ssh -o PubkeyAuthentication=no <USUARIO>@<IP_DE_SU_VM> true; echo "salida: $?"
# <USUARIO>@<IP_DE_SU_VM>: Permission denied (publickey).
# salida: 255
```

---

## Paso 10 - Verlo fallar: el archivo con el nombre equivocado

Primero, el problema a mano. Simula lo que traen muchas imágenes de nube (un `50-cloud-init.conf` que deja las contraseñas encendidas):

```bash
echo "PasswordAuthentication yes" | sudo tee /etc/ssh/sshd_config.d/50-cloud-init.conf
sudo mv /etc/ssh/sshd_config.d/10-endurecer.conf /etc/ssh/sshd_config.d/99-endurecer.conf
sudo sshd -t && echo "sintaxis OK"
sudo sshd -T | grep passwordauthentication
```

Salida real:

```
sintaxis OK
passwordauthentication yes
```

**Lo que debes ver:** la sintaxis está bien y la contraseña sigue entrando. Gana el archivo que va **primero** en orden léxico.

Deshaz la prueba y deja el rol arreglar el desorden:

```bash
sudo rm /etc/ssh/sshd_config.d/99-endurecer.conf
ansible-playbook site.yml -K --tags ssh -e endurecer_ssh_dropin=99-endurecer.conf
```

Salida real:

```
TASK [endurecer : Comprobar que ningun otro .conf pisa el endurecimiento]
[ERROR]: Task failed: Action failed: sshd -T no muestra el valor endurecido: otro archivo de sshd_config.d lo esta ganando.
fatal: [vm-lab]: FAILED! => {"assertion": "'passwordauthentication no' in endurecer_sshd_efectivo.stdout_lines", ...}

PLAY RECAP ***
vm-lab : ok=5    changed=2    unreachable=0    failed=1    skipped=0    rescued=0    ignored=0
```

El rol **falló en voz alta** en lugar de dar el servidor por endurecido. Quita el archivo equivocado y vuelve a correr sin `-e`:

```bash
sudo rm /etc/ssh/sshd_config.d/99-endurecer.conf
ansible-playbook site.yml -K --tags ssh
ansible-playbook site.yml -K --tags ssh
```

Resumen esperado de la primera: `failed=0`, `changed=2` (recrea el drop-in y recarga). De la segunda: `changed=0`.

---

## Paso 11 - Verlo fallar: una variable mal escrita

Edita `roles/endurecer/tasks/ufw.yml` y cambia `endurecer_ufw_puertos_tcp` por `endurecer_ufw_puerto_tcp` (sin la "s") en el `loop`. Corre:

```bash
ansible-playbook site.yml -K --tags ufw
```

Debes ver un error con `is undefined` en el nombre de la variable que escribiste mal. Corrígelo y vuelve a correr hasta `failed=0`.

---

## Evidencia a entregar

Un directorio `evidencia-s04/` en tu repositorio con:

1. `corrida-1.txt` y `corrida-2.txt`: el `PLAY RECAP` de la primera y segunda corrida de `site.yml` (la segunda con `changed=0`).
2. `mentiroso.txt`: la salida de dos corridas de `no-idempotente.yml`, con `changed=2` en ambas.
3. `sshd-efectivo.txt`: la salida de `sudo sshd -T | grep -E '^(passwordauthentication|permitrootlogin) '`.
4. `ufw.txt`: la salida de `sudo ufw status verbose`.
5. `fallo-99.txt`: la salida donde la `assert` detiene el rol con el archivo `99-`.
6. El rol completo (`roles/endurecer/`) y `site.yml` en un commit.

---

## Si algo falla

| Síntoma | Qué revisar |
|---|---|
| `community.general.ufw was not found` | Corre `ansible-galaxy collection install -r requirements.yml -p colecciones` y revisa `collections_path` en `ansible.cfg` |
| `Missing sudo password` | Agrega `-K` (pide la contraseña de sudo) |
| `Permission denied (publickey)` al entrar por SSH | No sigas con el rol hasta tener acceso por llave; usa la segunda terminal o la consola de la VM |
| Te quedaste fuera por UFW | Desde la consola de la VM: `sudo ufw disable`, corrige la lista de puertos y vuelve a correr |
| `changed` en la segunda corrida | Busca la tarea con `changed:` en la salida: un `command` sin `changed_when`, o una plantilla con contenido distinto en cada corrida |
| `Failed to reload ssh` | `sudo sshd -t` te dice la línea con el error de sintaxis |
| El rol no se encuentra | Estás en la carpeta equivocada: debe ser `~/iac-lab/ansible` (donde está `ansible.cfg`) |
