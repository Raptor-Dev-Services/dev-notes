<!-- _class: lead -->

# S05 - Calidad y CI para Ansible

### Infraestructura como código

**Rogelio Arriaga - Raptor Dev Services**

<!--
Bienvenida. Cada equipo trae el rol endurecer de la sesión 4 funcionando y la segunda corrida con changed=0. Hoy lo sometemos a pruebas que no dependen de la memoria de nadie. Si alguien no terminó la tarea, trabaja con el rol de la práctica de S04.
-->

---

## Agenda del día

| Bloque | Qué hacemos | Tiempo aprox. |
|---|---|---|
| Apertura | Lema, objetivos, repaso del rol | 20 min |
| Parte 1 | Depurar un playbook sin adivinar | 40 min |
| Parte 2 | ansible-lint: reglas y perfiles | 40 min |
| Parte 3 | Molecule: probar el rol en un servidor limpio | 60 min |
| Parte 4 | CI y buenas prácticas (nombres, tags, vault, colecciones) | 40 min |
| Parte 5 | Práctica: Molecule y CI para el rol endurecer | 70 min |
| Cierre | Errores comunes, tarea y quiz | 30 min |

<!-- Hay un descanso de 10 minutos entre la Parte 2 y la Parte 3, y otro a la mitad de la práctica. Ya están dentro de los tiempos. -->

---

## El lema de la sesión

> Un rol sin prueba se rompe la primera vez que alguien más lo usa.

- Tu rol funciona en **tu** VM, con **tus** paquetes y **tu** historia de comandos a mano.
- Alguien más lo corre en un servidor limpio y falla por algo que tu máquina ya tenía.
- Hoy el servidor limpio es un contenedor que se crea, se prueba y se destruye en cada cambio.

<!-- Pregunta al grupo: ¿alguna vez un script suyo "funcionó en mi máquina" y falló en otra? ¿Qué tenía su máquina que la otra no? -->

---

## Al terminar puedes

- Depurar un playbook con `-v`, `debug`, `--list-tasks`, `--start-at-task` y `--step`.
- Correr `ansible-lint`, leer sus reglas y elegir un perfil.
- Escribir un escenario de Molecule que crea un servidor limpio, aplica el rol, comprueba la idempotencia y verifica el resultado.
- Ver fallar la idempotencia y la verificación **a propósito** y leer el mensaje.
- Poner todo en un workflow de GitHub Actions que detiene los cambios malos en cada pull request.
- Guardar un secreto con `ansible-vault` sin dejarlo en la salida.

---

## Versiones de esta sesión (a noviembre de 2026)

| Herramienta | Versión fijada | Dónde se comprobó |
|---|---|---|
| ansible-core | 2.21.6 | PyPI (2026-11-02) y la tabla de soporte de Python |
| ansible-lint | 26.11.0 | PyPI y releases de GitHub (2026-11-05) |
| Molecule | 26.11.0 | PyPI y releases de GitHub (2026-11-05) |
| community.docker | 5.4.0 | Galaxy |
| community.general | 14.0.1 | Galaxy |

- Las pruebas de esta sesión se corrieron con estas versiones; si subes una, vuelve a correrlas.

---

<!-- _class: lead -->

# Parte 1
## Depurar un playbook sin adivinar

---

## El problema: "no funciona" no es un diagnóstico

Cuando un playbook falla, hay tres preguntas, en este orden:

1. **¿Qué tarea?** Ansible te la nombra: por eso los `name` importan.
2. **¿Con qué valores?** Variables, facts y el resultado de la tarea anterior.
3. **¿Qué pasaría si...?** Sin tocar nada: `--check --diff` y `--list-tasks`.

> Depurar es hacer visible lo que Ansible ya sabe.

Fuente: Meijer et al., *Ansible: Up and Running* (2022), cap. 8.

---

## Ver el plan sin ejecutarlo

```bash
ansible-playbook depurar.yml --list-tasks
ansible-playbook depurar.yml --list-tags
ansible-playbook depurar.yml --syntax-check
```

```text
  play #1 (localhost): Practicar la depuracion	TAGS: []
    tasks:
      Mostrar la lista de puertos	TAGS: []
      Leer la version del sistema	TAGS: []
      Mostrar solo el nombre bonito	TAGS: []
      Usar una variable que no existe	TAGS: [falla]
```

- `--list-tasks`: "list all tasks that would be executed". `--list-tags`: "list all available tags". `--syntax-check`: valida sin ejecutar.
- Salida real de ansible-core 2.21.6.

Fuente: página de `ansible-playbook` (CLI de Ansible).

---

## `debug`: mirar una variable

```yaml {1-4|6-10|*}
- name: Mostrar la lista de puertos
  ansible.builtin.debug:
    var: puertos

- name: Leer la version del sistema
  ansible.builtin.command: cat /etc/os-release
  register: os_release
  changed_when: false

- name: Mostrar solo el nombre bonito
  ansible.builtin.debug:
    msg: "{{ os_release.stdout_lines | select('match', '^PRETTY_NAME') | first }}"
```

- `var:` imprime el nombre y el valor. `msg:` evalúa una expresión Jinja2.
- `register` guarda **todo** el resultado de la tarea (`stdout`, `stdout_lines`, `rc`, `changed`...). Imprímelo completo una vez para saber qué trae.
- Quita los `debug` que no aporten antes del commit, o márcalos con una etiqueta `debug`.

---

## `-v` y compañía

| Opción | Qué agrega |
|---|---|
| `-v` | Más mensajes de depuración (cada `v` extra sube el nivel) |
| `-vvv` | Punto de partida razonable, según la documentación |
| `-vvvv` | Útil para depurar la conexión, según la documentación |

- Cada `v` extra suma ruido: empieza con `-v` y sube solo si hace falta.
- Con `no_log: true` en una tarea, el resultado **desaparece incluso del error**. Útil para secretos; molesto al depurar. Quítalo un momento **con un valor ficticio**, nunca con el secreto real.

Fuente: página de `ansible-playbook` (CLI).

---

## Un error real, leído línea por línea

```text
fatal: [localhost]: FAILED! => {"msg": "Task failed: Finalization of task args
for 'ansible.builtin.debug' failed: Error while resolving value for 'msg':
'puerto_ssh' is undefined"}
```

Cómo se lee:

- **Qué tarea** (arriba del `fatal`, con el nombre y el archivo:línea).
- **Qué módulo** (`ansible.builtin.debug`).
- **Qué argumento** (`msg`) y **por qué** (`'puerto_ssh' is undefined`).
- Ansible también imprime el trozo del YAML con una flecha `^ column 14` en la posición.

---

## Retomar donde se quedó, y paso a paso

```bash
ansible-playbook depurar.yml --start-at-task "Usar una variable que no existe"
ansible-playbook depurar.yml --step
```

- `--start-at-task`: "start the playbook at the task matching this name". Útil cuando las primeras 40 tareas ya pasaron.
- `--step`: "one-step-at-a-time: confirm each task before running". Pregunta antes de cada tarea.
- **Ojo:** si saltas tareas, las variables que ellas registraban **no existen**. Un `--start-at-task` puede fallar por eso, y no es un bug del rol.

---

## Error típico: el YAML mal sangrado

```text
[ERROR]: YAML parsing failed: While parsing a block collection did not find expected '-' indicator.
Origin: roto.yml:6:6

4       ansible.builtin.debug:
5         msg: hola
6      tags: [a]
       ^ column 6
```

- Una línea con un espacio de menos **rompe todo el archivo**; el error dice archivo, línea y columna.
- `--syntax-check` lo encuentra antes de tocar un servidor.
- Un editor con "mostrar espacios" y un linter de YAML evitan el 90 % de estos errores.

---

<!-- _class: lead -->

# Parte 2
## ansible-lint

---

## El problema: errores que no son errores todavía

Hay código que **funciona** y es peligroso o poco mantenible:

- `command` sin `changed_when`: funciona, pero miente en cada corrida.
- Una tarea sin `name`: funciona, pero la salida no dice nada.
- Un `shell` donde bastaba un módulo.

`ansible-lint` revisa playbooks y roles contra una lista de reglas y dice **qué regla** rompe cada línea.

```bash
ansible-lint roles/endurecer site.yml no-idempotente.yml
```

---

## Salida real de ansible-lint 26.11.0

```text
# Rule Violation Summary

  2 name profile:moderate tags:idiom
  2 no-changed-when profile:moderate tags:command-shell,idempotency

Failed: 4 failure(s), 0 warning(s) in 13 files processed of 13 encountered.
Last profile that met the validation criteria was 'basic'. Rating: 1/5 star

no-changed-when: Commands should not change things if nothing needs doing.
no-idempotente.yml:6 Task/Handler: Crear carpeta de datos

name[casing]: All names should start with an uppercase letter.
roles/endurecer/tasks/fail2ban.yml:18:9 Task/Handler: fail2ban arrancado y habilitado
```

- Cada hallazgo: **regla**, explicación corta, **archivo:línea** y tarea.
- `no-changed-when` es la regla que atrapa al playbook que miente de la sesión 4.
- Código de salida real: **2** con hallazgos y **0** con el código limpio. Eso es lo que lee el CI.

---

## Los perfiles

Un perfil es un nivel de exigencia; cada uno **incluye** al anterior:

| Perfil | Propósito (documentación de ansible-lint) |
|---|---|
| `min` | Que Ansible pueda cargar el contenido |
| `basic` | Prevenir errores comunes y exigir estilo y formato estándar |
| `moderate` | Buenas prácticas para que el contenido sea fácil de leer y mantener |
| `safety` | Evitar llamadas a módulos con resultados no deterministas o riesgos de seguridad |
| `shared` | Buenas prácticas de empaquetado y publicación |
| `production` | Requisitos para incluir contenido en Ansible Automation Platform |

```bash
ansible-lint --list-profiles
ansible-lint --profile=production roles/endurecer
```

- Con el rol corregido (`Fail2ban arrancado...` con mayúscula), real: `Passed: 0 failure(s), 0 warning(s) in 12 files processed of 12 encountered. Profile 'production' was required, and it passed.`

Fuente: "Profiles" y "Using ansible-lint" (docs.ansible.com/projects/lint).

---

## Cuando una regla no aplica

- **Configuración del proyecto:** archivo `.ansible-lint` en la raíz, con el perfil y las exclusiones.
- **`-x` / `--skip-list`:** omite reglas por ID o etiqueta.
- **`-w` / `--warn-list`:** las reporta como advertencia, sin fallar.
- **`# noqa: regla`** al final de la línea, **con un comentario que diga por qué**.
- **`--fix`:** aplica correcciones automáticas (por ejemplo, formato). Revisa el diff antes del commit.

> Una excepción sin motivo escrito es una deuda disfrazada. Exige el comentario en la revisión.

Fuente: "Using ansible-lint" (docs.ansible.com/projects/lint).

---

<!-- _class: lead -->

# Parte 3
## Molecule: probar el rol en un servidor limpio

---

## El problema: lo que tu máquina ya tenía

- Tu VM ya tenía `ufw` instalado, la llave en su lugar, el usuario creado.
- En un servidor nuevo, el rol falla porque asume algo que nadie puso.
- Queremos: **servidor limpio -> aplicar el rol -> aplicarlo otra vez -> comprobar el resultado -> destruir**.

Molecule hace exactamente esa secuencia, con tus propios playbooks.

La documentación de Molecule dice: "Molecule is an Ansible testing framework designed for developing and testing Ansible collections, playbooks, and roles." y que da soporte solo a las dos últimas versiones mayores de Ansible (N y N-1).

Fuente: docs.ansible.com/projects/molecule.

---

## La secuencia de pruebas

```text
dependency -> destroy -> create -> prepare -> converge -> idempotence -> verify -> destroy
```

| Paso | Qué hace | En nuestro escenario |
|---|---|---|
| dependency | Instala colecciones y roles | `requirements.yml` del escenario |
| destroy | Borra restos de una corrida anterior | Borra el contenedor |
| create | Crea el servidor de prueba | Un contenedor `ubuntu:24.04` |
| prepare | Lo que la imagen no trae | Python, sudo y OpenSSH por `raw` |
| converge | Aplica el rol | `include_role` |
| idempotence | Vuelve a aplicar y exige **cero cambios** | Falla si alguna tarea cambió |
| verify | Comprueba el estado final | `assert` sobre `sshd -T`, UFW y fail2ban |
| destroy | Borra el servidor | Siempre, falle o no |

La secuencia por omisión de la documentación es `dependency, destroy, create, converge, idempotence, verify, destroy`; `prepare` es opcional y aquí lo agregamos.

Fuente: "Getting started: roles" (docs.ansible.com/projects/molecule).

---

## Dónde vive el escenario

```text
roles/endurecer/
├── tasks/ handlers/ templates/ defaults/ meta/
└── molecule/
    └── default/
        ├── molecule.yml          configuración y secuencia
        ├── requirements.yml      colecciones del escenario
        ├── inventory/hosts.yml   el servidor de prueba
        ├── create.yml            levanta el contenedor
        ├── prepare.yml           deja la imagen lista
        ├── converge.yml          aplica el rol
        ├── verify.yml            comprueba
        └── destroy.yml           lo borra
```

- `molecule init scenario` crea la plantilla; **tú escribes** `create.yml`, `destroy.yml` y el inventario.
- Con Molecule 26.11.0 no hay clave `driver:` ni `platforms:` en `molecule.yml`: el servidor de prueba lo define **tu** `create.yml` y se conecta con un plugin de conexión (la documentación usa Podman; en el curso usamos Docker con `community.docker`).
- `molecule/` es andamio de prueba: **no es parte del rol**.

Fuente: "Getting started: roles" y salida de `molecule init scenario` con Molecule 26.11.0.

---

## `molecule.yml`

```yaml {1-5|7-14|16-27|*}
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

---

## El servidor de prueba: inventario y create

```yaml
# inventory/hosts.yml
---
all:
  children:
    instance:
      hosts:
        a07-molecule:
          ansible_connection: community.docker.docker
          ansible_user: root
```

```yaml
# create.yml
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

- `NET_ADMIN` permite que UFW toque las reglas dentro del contenedor.
- `community.docker` 5.4.0 habló con Docker **sin** instalar el paquete `docker` de Python (comprobado con `molecule test`).

---

## `prepare`, `converge` y `verify`

```yaml
# prepare.yml (la imagen base no trae Python, sudo ni OpenSSH)
- name: Prepare
  hosts: instance
  gather_facts: false
  tasks:
    - name: Python y sudo
      ansible.builtin.raw: apt-get update -qq && apt-get install -y -qq python3 sudo openssh-server
      changed_when: true
```

```yaml
# converge.yml
- name: Converge
  hosts: instance
  become: true
  tasks:
    - name: Aplicar el rol bajo prueba
      ansible.builtin.include_role:
        name: raptor_academy.endurecer
```

- `raw` no necesita Python en el destino: por eso instala Python.
- El rol se llama con su nombre **completo** `namespace.rol`, porque Molecule lo instala con el `namespace` de `meta/main.yml`.

---

## Verlo fallar (1): el nombre del rol

Sin `namespace` en `meta/main.yml`, Molecule se detiene antes de empezar:

```text
ansible_compat.errors.InvalidPrerequisiteError: Computed fully qualified role name of endurecer does not follow current galaxy requirements.
Please edit meta/main.yml and assure we can correctly determine full role name:
```

Con el `namespace` ya definido pero `name: endurecer` en `converge.yml`:

```text
[ERROR]: The role 'endurecer' was not found in: .../molecule/default/roles:...
```

- Arreglo: `role_name: endurecer` y `namespace: raptor_academy` en `meta/main.yml`, y `name: raptor_academy.endurecer` en `converge.yml`.

---

## `verify.yml`: probar el resultado, no los pasos

```yaml {1-9|11-15|17-22|*}
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

    - name: UFW activo, entrada denegada y 22/tcp permitido
      ansible.builtin.assert:
        that:
          - "'Status: active' in ufw_estado.stdout"
          - "'deny (incoming)' in ufw_estado.stdout"
          - "'22/tcp' in ufw_estado.stdout"
```

- `verify` comprueba **el estado final del servidor**, no que las tareas se hayan ejecutado.
- Una prueba que no puede fallar no prueba nada: en la práctica la verás fallar.

---

## Una corrida de `molecule test`

Salida real (resumen):

```text
default > dependency: Executed: Successful
default > destroy: Executed: Successful
default > create: Executed: Successful
default > prepare: Executed: Successful
default > converge: Executed: Successful      ok=15  changed=9
default > idempotence: Executed: Successful   ok=13  changed=0
default > verify: Executed: Successful
default > destroy: Executed: Successful
SCENARIO RECAP
default : actions=8  successful=7  disabled=0  skipped=0  missing=0  failed=0
```

- En este servidor cargado tardó cerca de **1 minuto 30 s** por corrida; cuenta con ese tiempo en el CI.
- Si algo falla, Molecule **destruye el contenedor igual** ("Cleaning up"): no se acumulan servidores de prueba.

---

## Verlo fallar (2): la idempotencia

Agrega al final de `tasks/ufw.yml` un `command` sin `changed_when`:

```yaml
- name: Registrar los rechazos en el log
  ansible.builtin.command: ufw logging low
```

Resultado real de `molecule test`:

```text
TASK [raptor_academy.endurecer : Registrar los rechazos en el log] *************
changed: [a07-molecule]

PLAY RECAP
a07-molecule : ok=13   changed=1    unreachable=0    failed=0

CRITICAL Idempotence test failed because of the following tasks:
*  => raptor_academy.endurecer : Registrar los rechazos en el log
ERROR    [default > idempotence] Executed: Failed
```

- Molecule dijo **qué tarea** cambió en la segunda pasada. El rol del curso la habría dejado pasar.

---

## El arreglo

```yaml
- name: Registrar los rechazos en el log
  community.general.ufw:
    logging: low
```

Segunda pasada real: `ok=13 changed=0`, y el recap final con `failed=0`.

- Un módulo declarativo (`community.general.ufw` con `logging: low`) **sabe** si el nivel ya es ese.
- La lección es la misma de la sesión 4, ahora **automática**: ningún cambio que rompa la idempotencia llega a la rama principal.

---

<!-- _class: lead -->

# Parte 4
## CI y buenas prácticas

---

## El problema: la prueba que nadie corre

- Molecule corre en tu laptop **si te acuerdas**.
- El pipeline la corre **siempre**, con las versiones fijadas, antes de que alguien fusione.
- En un pull request: lint primero (barato), Molecule después (caro).

```text
pull request -> lint (perfil production) -> molecule test -> revisión humana
```

---

## El workflow (FRAGMENTO)

> **Fragmento:** el YAML se validó con un parser y los SHA se obtuvieron de la API de GitHub; el workflow no se ejecutó en un runner real.

```yaml {1-9|11-23|25-39|*}
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

---

## Por qué está escrito así

- `paths`: el workflow corre si cambió el rol o el propio workflow.
- `permissions: contents: read`: el workflow solo lee. Lo que no declaras queda en `none`.
- Acciones fijadas por **SHA** con el tag en un comentario: `actions/checkout` v7.0.1 y `actions/setup-python` v7.1.0 (releases de GitHub, comprobadas con `gh api`).
- Herramientas fijadas por versión: una subida de `ansible-lint` no rompe tu CI sin que tú decidas subirla.
- `needs: lint`: si el lint falla, Molecule ni arranca. Ahorra minutos.
- Python 3.12 en el runner, como en la VM del curso.

Fuente: "Workflow syntax for GitHub Actions" (docs.github.com).

---

## Qué detiene el CI y cómo

| Cambio en el pull request | Quién lo detiene | Señal |
|---|---|---|
| Tarea sin `changed_when` | ansible-lint | Exit 2, `no-changed-when` |
| Nombre de tarea en minúscula | ansible-lint | `name[casing]` |
| Tarea que cambia en la segunda pasada | Molecule (`idempotence`) | `Idempotence test failed` |
| Control de seguridad que dejó de cumplirse | Molecule (`verify`) | `assert` fallida |
| Un `.conf` que gana sobre el endurecimiento | La `assert` del rol y `verify` | Mensaje de `sshd -T` |

- Configura la rama principal para que **exija** que el workflow pase antes de fusionar.
- Un CI que se puede ignorar es un adorno.

---

## Buenas prácticas

- **Nombres:** `name:` en cada tarea, con mayúscula y diciendo el **resultado** ("Jaula sshd"), no el comando.
- **Tags** con criterio (`ssh`, `ufw`, `fail2ban`) para correr una parte.
- **FQCN** siempre; colecciones fijadas en `requirements.yml`.
- **Módulos antes que `command`/`shell`.**
- **Variables con prefijo** y `defaults/` documentados.
- **Un rol, una responsabilidad.**
- **Verificación incluida:** una `assert` por control crítico.

Fuente: Meijer et al., *Ansible: Up and Running* (2022), cap. 24 (Best Practices).

---

## Secretos: `ansible-vault`

Cifrar un valor individual (la clave de la clase es ficticia):

```bash
echo -n 'contrasena-ficticia-123' | ansible-vault encrypt_string \
  --vault-password-file .vault-pass --stdin-name 'db_password'
```

```text
db_password: !vault |
          $ANSIBLE_VAULT;1.1;AES256
          64373434373335373038373035303365343539366635646561356534653865343663623264...
```

- El texto cifrado **sí** puede ir al repositorio; la **clave del vault** (`.vault-pass`) **no**: va a `.gitignore` y se comparte por un gestor de secretos.
- Se usa como cualquier variable (`vars_files`) y se pasa la clave con `--vault-password-file`.
- La documentación advierte: escribir el secreto directo en la línea de comandos lo deja en el historial del shell; `decrypt` deja el archivo en claro en disco; los editores dejan copias.

Fuente: "Encrypting content with Ansible Vault" (docs.ansible.com, Vault guide).

---

## Verlo fallar: el secreto que se imprime y el que desaparece

```yaml
- name: Comprobar que se descifra sin imprimirlo
  ansible.builtin.assert:
    that: db_password | length > 0
  no_log: true
```

- Con la clave correcta: `ok=1`. Sin `--vault-password-file`, el playbook falla y el mensaje es:

```text
fatal: [localhost]: FAILED! => {"censored": "the output has been hidden due to the fact that 'no_log: true' was specified for this result", "changed": false}
```

- `no_log: true` protege el secreto **y también el diagnóstico**: sabes que falló, no por qué. Para depurar, prueba temporalmente con un valor ficticio.
- Un secreto cifrado con vault sigue siendo un secreto: **rota la clave** si alguien que la conocía sale del equipo (`ansible-vault rekey` en archivos completos).

---

## Colecciones: fijar y no sorprenderse

```yaml
---
collections:
  - name: community.general
    version: "14.0.1"
```

```bash
ansible-galaxy collection install -r requirements.yml -p colecciones
```

- Una colección sin versión es "la última de hoy": mañana puede cambiar el comportamiento de tu módulo.
- Fijar la versión **y** correr Molecule es lo que permite actualizar con confianza: subes la versión, corre el CI, lees el resultado.
- Revisa los *changelogs* de la colección antes de subir de versión mayor.

---

<!-- _class: lead -->

# Parte 5
## Práctica: Molecule y CI para el rol endurecer

---

## Lo que vamos a hacer

1. Agregar el escenario `default` de Molecule al rol `endurecer`.
2. Correr `molecule test` hasta verlo en verde.
3. **Verlo fallar:** un cambio al rol que rompe la idempotencia, leer el mensaje y corregir.
4. Pasar `ansible-lint --profile production` y arreglar lo que salga.
5. Escribir el workflow de CI (el YAML se valida con un parser) y **verlo detener** el cambio malo.
6. Entrega: el rol con su escenario, el workflow y la evidencia de las dos fallas.

---

## Errores comunes de la sesión

| Síntoma | Causa | Qué hacer |
|---|---|---|
| `Computed fully qualified role name ... does not follow current galaxy requirements` | Falta `namespace` o `role_name` en `meta/main.yml` | Agregarlos |
| `The role 'endurecer' was not found` | `include_role` con el nombre corto | Usar `namespace.rol` |
| `/usr/bin/python3: not found` en `converge` | Falta `prepare.yml` con Python | Instalarlo con `raw` |
| `Idempotence test failed` | Una tarea cambia en la segunda pasada | Módulo declarativo o `changed_when` |
| El CI pasa y el servidor falla | El escenario no comprueba el resultado | Más `assert` en `verify.yml` |
| El lint falla solo en el CI | Otra versión de `ansible-lint` | Fijar versiones en el workflow |
| `UFW` falla en el contenedor | Falta `NET_ADMIN` | `capabilities: [NET_ADMIN]` en `create.yml` |

---

## Lo que Molecule no cubre

- No prueba **tu** VM real: prueba un contenedor Ubuntu 24.04 sin systemd.
- Un contenedor no es una máquina virtual: el kernel es el del anfitrión y algunos servicios se comportan distinto.
- Por eso la última verificación sigue siendo correr el rol en la VM y mirar `sshd -T`, `ufw status` y `fail2ban-client status`.
- Si necesitas probar con systemd real (arranque, reinicios), un escenario con una VM descartable es la herramienta; cuesta más tiempo.

> Una prueba en contenedor reduce el riesgo; no lo elimina.

---

## Referencias (a noviembre de 2026)

- Molecule, inicio: https://docs.ansible.com/projects/molecule/
- Molecule, roles: https://docs.ansible.com/projects/molecule/getting-started-roles/
- ansible-lint, perfiles: https://docs.ansible.com/projects/lint/profiles/
- ansible-lint, uso: https://docs.ansible.com/projects/lint/usage/
- Ansible Vault: https://docs.ansible.com/projects/ansible/latest/vault_guide/vault_encrypting_content.html
- Página de `ansible-playbook`: https://docs.ansible.com/projects/ansible/latest/cli/ansible-playbook.html
- Sintaxis de workflows de GitHub Actions: https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax
- Versiones: ansible-core 2.21.6, ansible-lint 26.11.0, Molecule 26.11.0, community.docker 5.4.0, community.general 14.0.1, actions/checkout v7.0.1, actions/setup-python v7.1.0.

---

## Tarea y siguiente sesión

**Tarea:** un cambio al rol que el CI detenga, y su corrección (ver `tarea.md`).

### S06 - Políticas como código

- Qué es una política como código y por qué rechaza **antes** de que exista el recurso.
- Open Policy Agent, Rego y Conftest.
- Leer el plan de OpenTofu como JSON y rechazarlo con una política.
- Excepciones, pruebas de políticas y el proyecto final.

> La política correcta rechaza el bucket público antes de que exista, no después del incidente.

---

## Quiz: Calidad y CI para Ansible

- 10 preguntas por intento, sacadas de un banco de 12.
- Individual, sin la VM abierta.
- Lo que falles, búscalo en esta clase y en la nota `09-cicd/06-ansible` antes del siguiente intento.

<!-- Últimos 15 minutos. Revisa en voz alta la pregunta que más se falle. -->
