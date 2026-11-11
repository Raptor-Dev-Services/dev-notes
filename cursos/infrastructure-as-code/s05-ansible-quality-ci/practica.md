# Práctica - S05 - Calidad y CI para Ansible

**Infraestructura como código**
**Duración:** 70 minutos, por equipo, en su VM.

---

## Objetivo

Probar el rol `endurecer` de la sesión 4 con **Molecule** en un contenedor limpio, pasar `ansible-lint` con el perfil `production` y poner las dos puertas en un workflow de CI. Ver fallar, a propósito:

1. Un `command` sin `changed_when` que ansible-lint rechaza.
2. Una tarea que rompe la **idempotencia** y que Molecule detecta en la segunda pasada.
3. Un control de seguridad que deja de cumplirse y que `verify.yml` detecta.

Versiones de esta guía (a noviembre de 2026): ansible-core 2.21.6, ansible-lint 26.11.0, Molecule 26.11.0, community.docker 5.4.0, community.general 14.0.1. Las salidas son de corridas reales con esas versiones.

---

## Antes de empezar

- El rol `endurecer` de la sesión 4, en `~/iac-lab/ansible/roles/endurecer`, con su `meta/main.yml` que lleva `role_name: endurecer` y `namespace: raptor_academy`.
- Docker en tu VM y tu usuario con permiso para usarlo:

```bash
docker --version
docker run --rm hello-world | head -3
```

- Un repositorio en GitHub con tu proyecto (de las sesiones anteriores) para el paso del CI. Si no lo tienes, corre en tu VM los mismos comandos que pone el workflow: el aprendizaje es el mismo.

> Molecule crea un contenedor y lo borra. Si tu VM tiene poca RAM, cierra otros contenedores antes de empezar. Una corrida tardó cerca de 1 minuto 30 segundos en un servidor cargado.

---

## Paso 1 - Instalar las herramientas

```bash
~/.venvs/ansible/bin/pip install "ansible-lint==26.11.0" "molecule==26.11.0"
export PATH="$HOME/.venvs/ansible/bin:$PATH"
ansible-lint --version | head -1
```

Salida esperada (las versiones de `ansible-core` y `ansible-compat` pueden variar un poco):

```
ansible-lint 26.11.0 using ansible-core:2.21.6 ansible-compat:26.11.0 ruamel-yaml:0.19.1
```

---

## Paso 2 - Verlo fallar: el lint rechaza al playbook que miente

Desde `~/iac-lab/ansible`, con el `no-idempotente.yml` de la sesión 4:

```bash
ansible-lint no-idempotente.yml; echo "salida: $?"
```

Salida real, recortada:

```
no-changed-when: Commands should not change things if nothing needs doing.
no-idempotente.yml:6 Task/Handler: Crear carpeta de datos

no-changed-when: Commands should not change things if nothing needs doing.
no-idempotente.yml:9 Task/Handler: Anotar la fecha de configuracion

Failed: 2 failure(s), 0 warning(s) ...
salida: 2
```

**Lo que debes ver:** las dos tareas que mienten, con su archivo y su línea, y la salida `2`. El workflow lee ese código de salida.

Ahora el rol, con el perfil más exigente:

```bash
ansible-lint --profile production roles/endurecer site.yml
echo "salida: $?"
```

Salida real con el rol de la sesión 4 corregido:

```
Passed: 0 failure(s), 0 warning(s) in 12 files processed of 12 encountered. Profile 'production' was required, and it passed.
salida: 0
```

Si te sale `name[casing]` (nombres de tarea que empiezan en minúscula), corrígelos. Es el hallazgo más común.

---

## Paso 3 - El escenario de Molecule

```bash
cd ~/iac-lab/ansible/roles/endurecer
molecule init scenario
find molecule -type f | sort
```

Salida esperada del `find`:

```
molecule/default/converge.yml
molecule/default/create.yml
molecule/default/destroy.yml
molecule/default/molecule.yml
molecule/default/verify.yml
```

Sustituye cada archivo por los siguientes (los de la plantilla traen marcas "TODO" y rutas de ejemplo).

`molecule/default/molecule.yml`:

```yaml
---
dependency:
  name: galaxy
  options:
    requirements-file: ${MOLECULE_SCENARIO_DIRECTORY}/requirements.yml

ansible:
  cfg:
    defaults:
      host_key_checking: false
    ssh_connection:
      pipelining: true
  executor:
    args:
      ansible_playbook:
        - --diff
        - --inventory=${MOLECULE_SCENARIO_DIRECTORY}/inventory/hosts.yml

scenario:
  name: default
  test_sequence:
    - dependency
    - destroy
    - create
    - prepare
    - converge
    - idempotence
    - verify
    - destroy
```

`molecule/default/requirements.yml`:

```yaml
---
collections:
  - name: community.docker
    version: "5.4.0"
  - name: community.general
    version: "14.0.1"
```

`molecule/default/inventory/hosts.yml` (crea la carpeta `inventory`):

```yaml
---
all:
  children:
    instance:
      hosts:
        a07-molecule:
          ansible_connection: community.docker.docker
          ansible_user: root
```

`molecule/default/create.yml`:

```yaml
---
- name: Create
  hosts: localhost
  connection: local
  gather_facts: false
  tasks:
    - name: Levantar el contenedor de prueba
      community.docker.docker_container:
        name: a07-molecule
        image: docker.io/library/ubuntu:24.04
        command: sleep infinity
        capabilities:
          - NET_ADMIN
        state: started
        pull: missing
```

`molecule/default/destroy.yml`:

```yaml
---
- name: Destroy
  hosts: localhost
  connection: local
  gather_facts: false
  tasks:
    - name: Borrar el contenedor de prueba
      community.docker.docker_container:
        name: a07-molecule
        state: absent
        force_kill: true
```

`molecule/default/prepare.yml`:

```yaml
---
- name: Prepare
  hosts: instance
  gather_facts: false
  tasks:
    - name: Python y sudo (la imagen base no los trae)
      ansible.builtin.raw: apt-get update -qq && apt-get install -y -qq python3 sudo openssh-server
      changed_when: true

    - name: Directorio de separacion de privilegios de sshd
      ansible.builtin.raw: mkdir -p /run/sshd
      changed_when: true

    - name: Simular el archivo de cloud-init que traen las imagenes de nube
      ansible.builtin.raw: mkdir -p /etc/ssh/sshd_config.d && echo "PasswordAuthentication yes" > /etc/ssh/sshd_config.d/50-cloud-init.conf
      changed_when: true
```

`molecule/default/converge.yml`:

```yaml
---
- name: Converge
  hosts: instance
  become: true
  tasks:
    - name: Aplicar el rol bajo prueba
      ansible.builtin.include_role:
        name: raptor_academy.endurecer
```

`molecule/default/verify.yml`:

```yaml
---
- name: Verify
  hosts: instance
  become: true
  gather_facts: false
  tasks:
    - name: Configuracion efectiva de sshd
      ansible.builtin.command: /usr/sbin/sshd -T
      register: sshd_efectivo
      changed_when: false

    - name: SSH sin contrasena y sin root
      ansible.builtin.assert:
        that:
          - "'passwordauthentication no' in sshd_efectivo.stdout_lines"
          - "'permitrootlogin no' in sshd_efectivo.stdout_lines"

    - name: Estado de UFW
      ansible.builtin.command: ufw status verbose
      register: ufw_estado
      changed_when: false

    - name: UFW activo, entrada denegada y 22/tcp permitido
      ansible.builtin.assert:
        that:
          - "'Status: active' in ufw_estado.stdout"
          - "'deny (incoming)' in ufw_estado.stdout"
          - "'22/tcp' in ufw_estado.stdout"

    - name: Jaula sshd de fail2ban
      ansible.builtin.stat:
        path: /etc/fail2ban/jail.d/10-endurecer.local
      register: jaula

    - name: La jaula existe
      ansible.builtin.assert:
        that: jaula.stat.exists
```

---

## Paso 4 - La primera corrida

```bash
molecule test
```

Salida real, resumen final:

```
default > dependency: Executed: Successful
default > destroy: Executed: Successful
default > create: Executed: Successful
default > prepare: Executed: Successful
default > converge: Executed: Successful
default > idempotence: Executed: Successful
default > verify: Executed: Successful
default > destroy: Executed: Successful
SCENARIO RECAP
default : actions=8  successful=7  disabled=0  skipped=0  missing=0  failed=0
```

Entre medias verás los `PLAY RECAP`: el de `converge` tiene `changed` mayor que cero (por ejemplo `ok=15 changed=9`) y el de `idempotence` tiene `changed=0` (por ejemplo `ok=13 changed=0`).

Al terminar no debe quedar ningún contenedor:

```bash
docker ps -a --filter name=a07-molecule -q | wc -l
# 0
```

Anota cuánto tardó (`time molecule test`).

---

## Paso 5 - Verlo fallar: el nombre del rol

Quita temporalmente `namespace: raptor_academy` de `meta/main.yml` y corre `molecule test`. Debes ver:

```
ansible_compat.errors.InvalidPrerequisiteError: Computed fully qualified role name of endurecer does not follow current galaxy requirements.
```

Devuélvelo. Después cambia en `converge.yml` `raptor_academy.endurecer` por `endurecer` y corre otra vez: el error es `The role 'endurecer' was not found in: ...`. Devuélvelo también.

---

## Paso 6 - Verlo fallar: la idempotencia

Agrega al final de `tasks/ufw.yml`:

```yaml

- name: Registrar los rechazos en el log
  ansible.builtin.command: ufw logging low
```

```bash
molecule test
```

Salida real (recortada):

```
TASK [raptor_academy.endurecer : Registrar los rechazos en el log] *************
changed: [a07-molecule]

PLAY RECAP *********************************************************************
a07-molecule : ok=13   changed=1    unreachable=0    failed=0    skipped=0    rescued=0    ignored=0

CRITICAL Idempotence test failed because of the following tasks:
*  => raptor_academy.endurecer : Registrar los rechazos en el log
ERROR    [default > idempotence] Executed: Failed
```

**Lo que debes ver:** Molecule nombra la tarea que cambió en la segunda pasada, y borra el contenedor aunque falló ("Cleaning up" y `destroy`).

El mismo cambio, ahora con `ansible-lint`:

```bash
ansible-lint --profile production .
```

Debe señalar `no-changed-when` en esa tarea. **Dos puertas distintas ven el mismo error.** Corrígelo con el módulo:

```yaml
- name: Registrar los rechazos en el log
  community.general.ufw:
    logging: low
```

Vuelve a correr `molecule test`: la segunda pasada reporta `changed=0` y el recap final dice `failed=0`.

---

## Paso 7 - Verlo fallar: el verify

Cambia en `defaults/main.yml` la lista de puertos a vacía:

```yaml
endurecer_ufw_puertos_tcp: []
```

(El rol no abre ningún puerto y activa UFW con entrada denegada.)

```bash
molecule test
```

Salida real (recortada):

```
TASK [UFW activo, entrada denegada y 22/tcp permitido] *************************
fatal: [a07-molecule]: FAILED! => {
    "assertion": "'22/tcp' in ufw_estado.stdout",
    "changed": false,
    "evaluated_to": false,
    "msg": "Assertion failed"
}

PLAY RECAP *********************************************************************
a07-molecule : ok=3    changed=0    unreachable=0    failed=1    skipped=0    rescued=0    ignored=0

ERROR    [default > verify] Executed: Failed
SCENARIO RECAP
default : actions=9  successful=6  disabled=0  skipped=0  missing=1  failed=1
```

**Lo que debes ver:** `converge` e `idempotence` pasan (el rol "corre bien"), pero `verify` falla porque `22/tcp` no está en las reglas de UFW. La línea `missing=1` es la etapa `cleanup`, que el escenario no define; es normal. El código de salida de `molecule test` fue 2.

Devuelve el valor `[22]` y corre de nuevo hasta ver `failed=0`.

> Esto es justo lo que Molecule hace por ti: un rol que "corre sin error" pero deja el firewall sin SSH **falla en verify**, antes de llegar a una VM real.

---

## Paso 8 - El workflow

En la raíz de tu repositorio, crea `.github/workflows/rol-endurecer.yml` con el workflow de la clase (la clase lo marcó como **fragmento**: lo valida un parser, no un runner):

```yaml
name: Rol endurecer

on:
  pull_request:
    paths:
      - "roles/endurecer/**"
      - ".github/workflows/rol-endurecer.yml"

permissions:
  contents: read

jobs:
  lint:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1
        with:
          persist-credentials: false
      - uses: actions/setup-python@6d4fd22b20465ccc0f7354476588e8df7c9ca0e0  # v7.1.0
        with:
          python-version: "3.12"
      - name: Instalar las herramientas fijadas
        run: python -m pip install "ansible-core==2.21.6" "ansible-lint==26.11.0"
      - name: ansible-lint con el perfil production
        run: ansible-lint --profile production roles/endurecer

  molecule:
    needs: lint
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: roles/endurecer
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1  # v7.0.1
        with:
          persist-credentials: false
      - uses: actions/setup-python@6d4fd22b20465ccc0f7354476588e8df7c9ca0e0  # v7.1.0
        with:
          python-version: "3.12"
      - name: Instalar las herramientas fijadas
        run: python -m pip install "ansible-core==2.21.6" "molecule==26.11.0"
      - name: Molecule (converge, idempotence y verify en un contenedor limpio)
        run: molecule test
```

Valida el YAML **antes** de empujar (un `:` sin comillas en un `run:` ha roto workflows reales):

```bash
~/.venvs/ansible/bin/python -c "import yaml,sys; d=yaml.safe_load(open('.github/workflows/rol-endurecer.yml')); print('jobs:', list(d['jobs']))"
```

Salida esperada:

```
jobs: ['lint', 'molecule']
```

> Aviso de YAML: el analizador de Python lee la clave `on` como el booleano `True`; GitHub la entiende como `on`. No es un error del archivo.

Verifica también que los SHA siguen siendo los de los tags (si tienes `gh` instalado y autenticado):

```bash
gh api repos/actions/checkout/git/ref/tags/v7.0.1 --jq '.object.sha'
```

---

## Paso 9 - Ver al CI detener el cambio

1. Crea una rama `prueba-ci`, reintroduce el `command` sin `changed_when` del paso 6 y abre un pull request.
2. El job `lint` debe fallar con `no-changed-when` y el job `molecule` **no** debe arrancar (por `needs: lint`).
3. Corrige el cambio, empuja y ve los dos jobs en verde.
4. Configura la rama principal para exigir que el workflow pase antes de fusionar (protección de rama).

Si no tienes un repositorio donde probar, corre en la VM los dos comandos del workflow, en orden, y anota el código de salida de cada uno.

---

## Evidencia a entregar

Carpeta `evidencia-s05/` con:

1. `molecule-verde.txt`: el recap final de `molecule test` y el tiempo.
2. `lint-mentiroso.txt`: la salida y el código `2` de `ansible-lint no-idempotente.yml`.
3. `idempotencia-falla.txt`: el mensaje `Idempotence test failed` con la tarea.
4. `verify-falla.txt`: la assert que falló en el paso 7.
5. `workflow.yml` y `workflow-validado.txt`.
6. Una captura o salida del CI (o de tus comandos locales) con el cambio malo detenido y después en verde.

---

## Si algo falla

| Síntoma | Qué revisar |
|---|---|
| `Cannot connect to the Docker daemon` | Tu usuario no está en el grupo `docker` o el servicio no corre: `systemctl status docker` |
| `The role 'endurecer' was not found` | El nombre en `converge.yml` debe ser `raptor_academy.endurecer` |
| `/usr/bin/python3: not found` | Falta `prepare.yml` o su tarea con `raw` |
| UFW falla dentro del contenedor | `capabilities: [NET_ADMIN]` en `create.yml` |
| `molecule` no se encuentra | El entorno virtual no está en el `PATH`: `export PATH="$HOME/.venvs/ansible/bin:$PATH"` |
| Quedó un contenedor `a07-molecule` | Un `molecule destroy` (o `docker rm -f a07-molecule`) lo borra |
| El lint pasa en tu VM y falla en el CI | Versiones distintas de `ansible-lint`: fíjala igual en el workflow |
